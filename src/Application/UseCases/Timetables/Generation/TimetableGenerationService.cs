using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Application.Common.Scheduling;
using SmartTimetableGenerator.Domain.AcademicYears;
using SmartTimetableGenerator.Domain.Areas;
using SmartTimetableGenerator.Domain.Campuses;
using SmartTimetableGenerator.Domain.Common.Calendar;
using SmartTimetableGenerator.Domain.Courses;
using SmartTimetableGenerator.Domain.DayTypes;
using SmartTimetableGenerator.Domain.Spaces;
using SmartTimetableGenerator.Domain.StudyPlans;
using SmartTimetableGenerator.Domain.Teachers;
using SmartTimetableGenerator.Domain.TeachingAssignments;
using SmartTimetableGenerator.Domain.TimetableGeneration;
using SmartTimetableGenerator.Domain.Timetables;

namespace SmartTimetableGenerator.Application.UseCases.Timetables.Generation;

/// <summary>
/// Ejecuta una solicitud de generación: arma el problema desde el plan de estudios, asigna docentes,
/// llama al motor y guarda las clases en el horario.
/// </summary>
public interface ITimetableGenerationService
{
    Task RunAsync(GenerationJobId jobId, CancellationToken cancellationToken);
}

internal sealed class TimetableGenerationService(
    IApplicationDbContext dbContext,
    ITimetableSolver solver,
    TimeProvider timeProvider,
    ILogger<TimetableGenerationService> logger) : ITimetableGenerationService
{
    private const int MinSecondsPerShift = 10;

    public async Task RunAsync(GenerationJobId jobId, CancellationToken cancellationToken)
    {
        var job = await dbContext.GenerationJobs
            .WithSpecification(GenerationJobSpec.ById(jobId))
            .FirstOrDefaultAsync(cancellationToken);

        if (job is null || job.Status != GenerationJobStatus.Queued)
            return;

        job.Start(timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            await GenerateAsync(job, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            job.Cancel(timeProvider.GetUtcNow());
        }
#pragma warning disable CA1031 // Cualquier error debe quedar registrado en la solicitud
        catch (Exception ex)
#pragma warning restore CA1031
        {
            logger.LogError(ex, "Falló la generación {JobId}", jobId);
            job.Fail(timeProvider.GetUtcNow(), $"Error inesperado: {ex.Message}");
        }

        await dbContext.SaveChangesAsync(CancellationToken.None);
    }

    private async Task GenerateAsync(GenerationJob job, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow;
        var report = new StringBuilder();

        var timetable = await dbContext.Timetables.WithSpecification(TimetableSpec.ById(job.TimetableId)).FirstOrDefaultAsync(ct);
        if (timetable is null)
        {
            job.Fail(now(), "El horario no existe");
            return;
        }

        if (timetable.Status != TimetableStatus.Draft)
        {
            job.Fail(now(), "El horario no está en borrador; despublíquelo antes de generar");
            return;
        }

        var plan = await dbContext.StudyPlans.WithSpecification(StudyPlanSpec.ById(timetable.StudyPlanId)).FirstAsync(ct);
        var campuses = await dbContext.Campuses.Include(c => c.Shifts).ThenInclude(s => s.BellSchedules).ToListAsync(ct);
        var campus = campuses.Single(c => c.Id == timetable.CampusId);
        var defaultDayType = await dbContext.DayTypes.WithSpecification(DayTypeSpec.Default()).FirstOrDefaultAsync(ct);
        var grades = await dbContext.Grades.ToDictionaryAsync(g => g.Id, ct);
        var areas = await dbContext.Areas.Include(a => a.Subjects).ToListAsync(ct);
        var courses = await dbContext.Courses
            .Where(c => c.AcademicYearId == timetable.AcademicYearId && c.CampusId == timetable.CampusId)
            .ToListAsync(ct);
        var teachers = (await dbContext.Teachers.Where(t => t.IsActive).ToListAsync(ct))
            .Where(t => t.WorksAt(campus.Id))
            .ToList();
        var spaces = await dbContext.Spaces.Where(s => s.CampusId == campus.Id && s.IsActive).ToListAsync(ct);
        var courseIds = courses.Select(c => c.Id).ToHashSet();
        var assignments = (await dbContext.TeachingAssignments
                .Where(a => a.AcademicYearId == timetable.AcademicYearId)
                .ToListAsync(ct))
            .Where(a => courseIds.Contains(a.CourseId))
            .ToList();

        // Clases de otras sedes en el mismo periodo: ocupan a los docentes compartidos
        var otherTimetables = await dbContext.Timetables
            .Include(t => t.Lessons)
            .Where(t => t.AcademicPeriodId == timetable.AcademicPeriodId && t.Id != timetable.Id &&
                        t.CampusId != timetable.CampusId && t.Status != TimetableStatus.Archived)
            .AsNoTracking()
            .ToListAsync(ct);

        var subjectArea = areas.SelectMany(a => a.Subjects.Select(s => (s.Id, a.Id))).ToDictionary(x => x.Item1, x => x.Item2);
        var subjectNames = areas.SelectMany(a => a.Subjects).ToDictionary(s => s.Id, s => s.Name);
        string CourseName(Course c) => $"{grades[c.GradeId].ShortName}{c.Name}";

        // Horas de cada docente en otras sedes (cuentan para su carga máxima) y franjas ocupadas por hora real
        var teacherLoad = teachers.ToDictionary(t => t.Id, _ => 0);
        var teacherBusy = teachers.ToDictionary(t => t.Id, _ => new List<(DayOfWeek Day, TimeOnly Start, TimeOnly End)>());
        foreach (var lesson in otherTimetables.SelectMany(t => t.Lessons))
        {
            if (lesson.TeacherId is not { } tid || !teacherLoad.ContainsKey(tid))
                continue;

            teacherLoad[tid]++;
            var otherShift = FindShift(campuses, lesson.ShiftId);
            var block = otherShift is null ? null : BellFor(otherShift, defaultDayType?.Id)?.ClassPeriod(lesson.PeriodNumber);
            if (block is not null)
                teacherBusy[tid].Add((lesson.Day, block.Start, block.End));
        }

        // Ítems del plan a programar (regulares y en contrajornada) por curso
        var work = new List<WorkItem>();
        foreach (var course in courses)
        {
            foreach (var item in plan.ItemsForGrade(course.GradeId))
            {
                if (item.DeliveryMode == DeliveryMode.Transversal)
                    continue;

                var hours = item.HoursFor(timetable.AcademicPeriodId);
                if (hours <= 0)
                    continue;

                var shiftId = item.DeliveryMode == DeliveryMode.CounterShift ? item.TargetShiftId!.Value : course.ShiftId;
                work.Add(new WorkItem(course, item, hours, shiftId));
            }
        }

        // 1) Asignación académica: respeta las manuales, propone el resto por área y carga
        AssignTeachers(timetable.AcademicYearId, timetable.AcademicPeriodId, work, assignments, teachers, subjectArea, teacherLoad, report, CourseName, subjectNames);

        foreach (var w in work.Where(w => w.NewAssignment is not null))
            dbContext.TeachingAssignments.Add(w.NewAssignment!);

        // 2) Espacios especiales requeridos por la asignatura
        AssignSpecialSpaces(work, spaces, report, CourseName, subjectNames);

        // 3) Programación por jornada
        var locked = timetable.Lessons.Where(l => l.IsLocked).ToList();
        var placements = new List<LessonPlacement>();
        var unplacedTotal = 0;
        var shiftsWithWork = work.Select(w => w.ShiftId).Distinct().ToList();
        var secondsPerShift = Math.Max(MinSecondsPerShift, job.TimeLimitSeconds / Math.Max(1, shiftsWithWork.Count));
        var anyInfeasible = false;

        // Espacios ocupados por franja (incluye clases fijadas): evita que un salón compartido tumbe toda la generación
        var spaceOccupancy = locked
            .Where(l => l.SpaceId is not null)
            .Select(l => (l.ShiftId, l.Day, l.PeriodNumber, l.SpaceId!.Value))
            .ToHashSet();
        var sharedSpaceConflicts = 0;

        foreach (var shiftId in shiftsWithWork)
        {
            var shift = campus.FindShift(shiftId);
            var bell = shift is null ? null : BellFor(shift, defaultDayType?.Id);
            var shiftWork = work.Where(w => w.ShiftId == shiftId).ToList();

            if (shift is null || bell is null)
            {
                report.AppendLine(CultureInfo.InvariantCulture, $"- Jornada sin horario de timbre: se omitieron {shiftWork.Sum(w => w.Hours)} horas.");
                unplacedTotal += shiftWork.Sum(w => w.Hours);
                continue;
            }

            var days = shift.Days.ToDaysOfWeek();
            var periods = bell.ClassPeriodCount;
            var problem = BuildProblem(shift, bell, days, periods, shiftWork, courses, teachers, teacherBusy, locked, secondsPerShift);
            var solution = await solver.SolveAsync(problem, ct);

            report.AppendLine(CultureInfo.InvariantCulture,
                $"- Jornada {shift.Name}: {Describe(solution.Status)} en {solution.SolveSeconds:0.0} s, {shiftWork.Count} asignaturas por curso.");

            if (solution.Status is SchedulingStatus.Infeasible or SchedulingStatus.Unknown)
            {
                anyInfeasible = true;
                continue;
            }

            foreach (var hour in solution.Placed)
            {
                var w = shiftWork[hour.ItemIndex];
                var day = days[hour.Day];
                var period = hour.Period + 1;

                if (locked.Any(l => l.CourseId == w.Course.Id && l.SubjectId == w.Item.SubjectId && l.ShiftId == shiftId && l.Day == day && l.PeriodNumber == period))
                    continue; // ya está en el horario como clase fijada

                // En contrajornada el salón del curso puede ser de otro curso en esa jornada: se deja sin espacio salvo que haya uno especial
                var space = w.EffectiveSpaceId;
                if (space is { } sid && !spaceOccupancy.Add((shiftId, day, period, sid)))
                {
                    // El salón ya lo usa otro curso en esa franja (salón compartido): la clase queda sin espacio asignado
                    space = null;
                    sharedSpaceConflicts++;
                }

                placements.Add(new LessonPlacement(w.Course.Id, w.Item.SubjectId, w.TeacherId, space, shiftId, day, period));

                if (w.TeacherId is { } tid && bell.ClassPeriod(period) is { } block && teacherBusy.TryGetValue(tid, out var busyList))
                    busyList.Add((day, block.Start, block.End));
            }

            foreach (var (index, hours) in solution.UnplacedHoursByItem)
            {
                var w = shiftWork[index];
                unplacedTotal += hours;
                report.AppendLine(CultureInfo.InvariantCulture, $"  · Sin ubicar: {CourseName(w.Course)} · {subjectNames[w.Item.SubjectId]}: {hours} h");
            }
        }

        if (sharedSpaceConflicts > 0)
        {
            report.AppendLine(CultureInfo.InvariantCulture,
                $"- {sharedSpaceConflicts} clases quedaron sin salón porque el salón está asignado a varios cursos en la misma franja. Revise los salones de los cursos.");
        }

        if (anyInfeasible)
        {
            job.MarkInfeasible(now(), "No existe un horario que cumpla todas las restricciones duras. El horario anterior se conservó.\n" + report);
            return;
        }

        var replaced = timetable.ReplaceUnlockedLessons(placements);
        if (replaced.IsError)
        {
            job.Fail(now(), $"El resultado tiene un conflicto: {replaced.FirstError.Description}\n{report}");
            return;
        }

        job.Complete(now(), placements.Count + locked.Count, unplacedTotal, report.ToString());
    }

    private static SchedulingProblem BuildProblem(
        Shift shift,
        BellSchedule bell,
        IReadOnlyList<DayOfWeek> days,
        int periods,
        List<WorkItem> shiftWork,
        List<Course> courses,
        List<Teacher> teachers,
        Dictionary<TeacherId, List<(DayOfWeek Day, TimeOnly Start, TimeOnly End)>> teacherBusy,
        List<Lesson> locked,
        int timeLimitSeconds)
    {
        static string CourseKey(CourseId id) => $"c:{id.Value}";
        static string TeacherKey(TeacherId id) => $"t:{id.Value}";
        static string SpaceKey(SpaceId id) => $"s:{id.Value}";

        // Un salón de curso que además es un espacio especial (p. ej. un laboratorio) debe respetarse como recurso
        var specialSpaces = shiftWork.Where(w => w.SpaceId is not null).Select(w => w.SpaceId!.Value).ToHashSet();

        var items = new List<SchedulingItem>();
        for (var i = 0; i < shiftWork.Count; i++)
        {
            var w = shiftWork[i];
            var resources = new List<string> { CourseKey(w.Course.Id) };
            if (w.TeacherId is { } tid) resources.Add(TeacherKey(tid));
            if (w.EffectiveSpaceId is { } sid && specialSpaces.Contains(sid)) resources.Add(SpaceKey(sid));

            var maxPerDay = Math.Min(w.Item.MaxHoursPerDay ?? w.Hours, periods);
            var maxConsecutive = Math.Min(w.Item.MaxConsecutiveHours ?? periods, periods);

            var fixedSlots = locked
                .Where(l => l.CourseId == w.Course.Id && l.SubjectId == w.Item.SubjectId && l.ShiftId == shift.Id)
                .Select(l => new SlotRef(IndexOf(days, l.Day), l.PeriodNumber - 1))
                .Where(s => s.Day >= 0 && s.Period < periods)
                .ToList();

            items.Add(new SchedulingItem(i, CourseKey(w.Course.Id), resources, w.Hours, maxPerDay, maxConsecutive,
                RequireContiguousPair: maxPerDay == 2 && maxConsecutive >= 2, fixedSlots));
        }

        var blocked = new List<BlockedSlot>();

        // Clases fijadas que no corresponden a un ítem del plan: bloquean sus recursos
        foreach (var l in locked.Where(l => l.ShiftId == shift.Id))
        {
            if (shiftWork.Any(w => w.Course.Id == l.CourseId && w.Item.SubjectId == l.SubjectId))
                continue;

            var d = IndexOf(days, l.Day);
            if (d < 0 || l.PeriodNumber > periods)
                continue;

            blocked.Add(new BlockedSlot(CourseKey(l.CourseId), d, l.PeriodNumber - 1));
            if (l.TeacherId is { } t) blocked.Add(new BlockedSlot(TeacherKey(t), d, l.PeriodNumber - 1));
            if (l.SpaceId is { } s) blocked.Add(new BlockedSlot(SpaceKey(s), d, l.PeriodNumber - 1));
        }

        var avoided = new List<BlockedSlot>();

        // Docentes: indisponibilidad declarada y clases en otras sedes/jornadas (por hora real)
        var teacherIds = shiftWork.Where(w => w.TeacherId is not null).Select(w => w.TeacherId!.Value).Distinct();
        foreach (var tid in teacherIds)
        {
            var teacher = teachers.FirstOrDefault(t => t.Id == tid);
            if (teacher is null)
                continue; // docente asignado a mano que no está registrado en esta sede

            var busy = teacherBusy[tid]
                .Concat(teacher.Availability.Where(r => r.Kind == AvailabilityKind.Unavailable).Select(r => (r.Day, r.Start, r.End)));

            foreach (var (day, start, end) in busy)
            {
                var d = IndexOf(days, day);
                if (d < 0) continue;

                for (var p = 1; p <= periods; p++)
                {
                    if (bell.ClassPeriod(p) is { } block && block.Start < end && start < block.End)
                        blocked.Add(new BlockedSlot(TeacherKey(tid), d, p - 1));
                }
            }

            // Franjas que prefiere evitar: preferencia blanda, el motor las usa solo si no hay otra opción
            foreach (var rule in teacher.Availability.Where(r => r.Kind == AvailabilityKind.Avoid))
            {
                var d = IndexOf(days, rule.Day);
                if (d < 0) continue;

                for (var p = 1; p <= periods; p++)
                {
                    if (bell.ClassPeriod(p) is { } block && block.Start < rule.End && rule.Start < block.End)
                        avoided.Add(new BlockedSlot(TeacherKey(tid), d, p - 1));
                }
            }
        }

        // Carga diaria: pareja para los cursos de esta jornada; máximo diario para los docentes
        var dailyLoads = new List<DailyLoadRule>();
        foreach (var course in courses.Where(c => c.ShiftId == shift.Id))
        {
            var total = shiftWork.Where(w => w.Course.Id == course.Id).Sum(w => w.Hours);
            if (total == 0) continue;

            var min = total / days.Count;
            var max = (total + days.Count - 1) / days.Count;
            dailyLoads.Add(new DailyLoadRule(CourseKey(course.Id), min, max));
        }

        // Máximo diario del docente, descontando lo que ya dicta ese día en otras jornadas o sedes
        foreach (var tid in teacherIds)
        {
            var teacher = teachers.FirstOrDefault(t => t.Id == tid);
            if (teacher?.MaxDailyHours is not { } maxDaily)
                continue;

            for (var d = 0; d < days.Count; d++)
            {
                var alreadyBusy = teacherBusy[tid].Count(b => b.Day == days[d]);
                dailyLoads.Add(new DailyLoadRule(TeacherKey(tid), 0, Math.Max(0, maxDaily - alreadyBusy), d));
            }
        }

        return new SchedulingProblem(days.Count, periods, items, blocked, dailyLoads, timeLimitSeconds, avoided);
    }

    private static void AssignTeachers(
        AcademicYearId yearId,
        AcademicPeriodId periodId,
        List<WorkItem> work,
        List<TeachingAssignment> assignments,
        List<Teacher> teachers,
        Dictionary<SubjectId, AreaId> subjectArea,
        Dictionary<TeacherId, int> teacherLoad,
        StringBuilder report,
        Func<Course, string> courseName,
        Dictionary<SubjectId, string> subjectNames)
    {
        TeachingAssignment? Existing(WorkItem w) =>
            assignments.FirstOrDefault(a => a.CourseId == w.Course.Id && a.SubjectId == w.Item.SubjectId && a.AcademicPeriodId == periodId) ??
            assignments.FirstOrDefault(a => a.CourseId == w.Course.Id && a.SubjectId == w.Item.SubjectId && a.AcademicPeriodId is null);

        // Asignaciones manuales primero
        foreach (var w in work)
        {
            var existing = Existing(w);
            if (existing is { IsManual: true, TeacherId: { } tid })
            {
                w.TeacherId = tid;
                w.Assignment = existing;
                if (teacherLoad.ContainsKey(tid))
                    teacherLoad[tid] += w.Hours;
            }
        }

        // Propuestas: mismo docente para la asignatura en los cursos del mismo grado cuando cabe
        var pending = work.Where(w => w.TeacherId is null)
            .GroupBy(w => (w.Course.GradeId, w.Item.SubjectId))
            .OrderByDescending(g => g.Sum(w => w.Hours));

        foreach (var group in pending)
        {
            var candidates = subjectArea.TryGetValue(group.Key.SubjectId, out var areaId)
                ? teachers.Where(t => t.CanTeach(areaId)).ToList()
                : [];
            var need = group.Sum(w => w.Hours);

            var shared = candidates
                .Where(t => teacherLoad[t.Id] + need <= t.MaxWeeklyHours)
                .OrderBy(t => teacherLoad[t.Id])
                .FirstOrDefault();

            foreach (var w in group)
            {
                var teacher = shared ?? candidates
                    .Where(t => teacherLoad[t.Id] + w.Hours <= t.MaxWeeklyHours)
                    .OrderBy(t => teacherLoad[t.Id])
                    .FirstOrDefault();

                if (teacher is null)
                {
                    report.AppendLine(CultureInfo.InvariantCulture,
                        $"- Sin docente disponible: {courseName(w.Course)} · {subjectNames[w.Item.SubjectId]} ({w.Hours} h). Se programa sin docente.");
                    continue;
                }

                w.TeacherId = teacher.Id;
                teacherLoad[teacher.Id] += w.Hours;
            }
        }

        // Guardar las propuestas como asignaciones académicas
        foreach (var w in work.Where(w => w.Assignment is null))
        {
            var existing = Existing(w);
            if (existing is null)
            {
                existing = TeachingAssignment.Create(yearId, w.Course.Id, w.Item.SubjectId);
                assignments.Add(existing);
                w.NewAssignment = existing;
            }

            existing.Suggest(w.TeacherId);
            w.Assignment = existing;
        }
    }

    private static void AssignSpecialSpaces(
        List<WorkItem> work,
        List<Space> spaces,
        StringBuilder report,
        Func<Course, string> courseName,
        Dictionary<SubjectId, string> subjectNames)
    {
        var usage = spaces.ToDictionary(s => s.Id, _ => 0);

        foreach (var w in work.Where(w => w.Item.RequiredSpaceType is not null).OrderByDescending(w => w.Hours))
        {
            var space = spaces
                .Where(s => s.Type == w.Item.RequiredSpaceType)
                .OrderBy(s => usage[s.Id])
                .FirstOrDefault();

            if (space is null)
            {
                report.AppendLine(CultureInfo.InvariantCulture,
                    $"- No hay espacio de tipo {w.Item.RequiredSpaceType} para {courseName(w.Course)} · {subjectNames[w.Item.SubjectId]}; se usa el salón del curso.");
                continue;
            }

            w.SpaceId = space.Id;
            usage[space.Id] += w.Hours;
        }
    }

    private static BellSchedule? BellFor(Shift shift, DayTypeId? dayTypeId) =>
        (dayTypeId is { } id ? shift.FindBellSchedule(id) : null) ?? shift.BellSchedules.FirstOrDefault();

    private static Shift? FindShift(IEnumerable<Campus> campuses, ShiftId shiftId) =>
        campuses.SelectMany(c => c.Shifts).FirstOrDefault(s => s.Id == shiftId);

    private static int IndexOf(IReadOnlyList<DayOfWeek> days, DayOfWeek day)
    {
        for (var i = 0; i < days.Count; i++)
        {
            if (days[i] == day)
                return i;
        }

        return -1;
    }

    private static string Describe(SchedulingStatus status) => status switch
    {
        SchedulingStatus.Optimal => "solución óptima",
        SchedulingStatus.Feasible => "solución válida (no se demostró óptima en el tiempo dado)",
        SchedulingStatus.Infeasible => "sin solución posible",
        _ => "sin solución en el tiempo dado"
    };

    private sealed class WorkItem(Course course, StudyPlanItem item, int hours, ShiftId shiftId)
    {
        public Course Course { get; } = course;
        public StudyPlanItem Item { get; } = item;
        public int Hours { get; } = hours;
        public ShiftId ShiftId { get; } = shiftId;
        public TeacherId? TeacherId { get; set; }
        public SpaceId? SpaceId { get; set; }

        /// <summary>Espacio especial requerido o, en su jornada regular, el salón del curso. En contrajornada no hay salón propio.</summary>
        public SpaceId? EffectiveSpaceId => SpaceId ?? (ShiftId == Course.ShiftId ? Course.HomeRoomId : null);

        public TeachingAssignment? Assignment { get; set; }
        public TeachingAssignment? NewAssignment { get; set; }
    }
}
