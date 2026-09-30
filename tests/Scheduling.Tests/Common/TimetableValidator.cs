using SmartTimetableGenerator.Domain.Campuses;
using SmartTimetableGenerator.Domain.Common.Calendar;
using SmartTimetableGenerator.Domain.Courses;
using SmartTimetableGenerator.Domain.StudyPlans;
using SmartTimetableGenerator.Domain.Teachers;
using SmartTimetableGenerator.Domain.TeachingAssignments;
using SmartTimetableGenerator.Domain.Timetables;

namespace SmartTimetableGenerator.Scheduling.Tests.Common;

/// <summary>
/// Revisa un horario generado contra el plan de estudios, los docentes, los espacios y el calendario de la sede,
/// de forma independiente al generador. Devuelve la lista de violaciones.
/// </summary>
public static class TimetableValidator
{
    public static IReadOnlyList<string> Validate(
        TestSchool school,
        Timetable timetable,
        IReadOnlyList<TeachingAssignment> assignments,
        IReadOnlyList<Lesson>? otherCampusLessons = null,
        bool expectComplete = true)
    {
        var errors = new List<string>();
        var lessons = timetable.Lessons;
        var subjects = new[] { school.Math, school.Humanities, school.Science, school.Technology, school.Sports, school.Ethics }
            .SelectMany(a => a.Subjects.Select(s => (Subject: s, Area: a)))
            .ToDictionary(x => x.Subject.Id);
        var courses = school.Courses.ToDictionary(c => c.Id);
        var teachers = school.Teachers.ToDictionary(t => t.Id);
        var spaces = school.Spaces.ToDictionary(s => s.Id);
        string Name(Course c) => $"{(c.GradeId == school.Sixth.Id ? "6º" : "7º")}{c.Name}";

        // 1) Cada clase en una franja real de su jornada
        foreach (var l in lessons)
        {
            var shift = school.Campus.FindShift(l.ShiftId);
            if (shift is null) { errors.Add($"Clase en jornada ajena a la sede: {l.Id}"); continue; }
            if (!shift.Days.Includes(l.Day)) errors.Add($"Clase en día sin clase ({l.Day}) en {shift.Name}");
            var bell = shift.FindBellSchedule(school.TypeA.Id)!;
            if (l.PeriodNumber < 1 || l.PeriodNumber > bell.ClassPeriodCount)
                errors.Add($"Franja {l.PeriodNumber} inexistente en {shift.Name}");
        }

        if (errors.Count > 0)
            return errors;

        TimeOnly Start(Lesson l) => school.Campus.FindShift(l.ShiftId)!.FindBellSchedule(school.TypeA.Id)!.ClassPeriod(l.PeriodNumber)!.Start;
        TimeOnly End(Lesson l) => school.Campus.FindShift(l.ShiftId)!.FindBellSchedule(school.TypeA.Id)!.ClassPeriod(l.PeriodNumber)!.End;

        // 2) Ningún curso, docente ni espacio en dos clases que se cruzan en el tiempo (incluye jornadas distintas)
        void NoOverlap<TKey>(string what, Func<Lesson, TKey?> key) where TKey : struct
        {
            foreach (var group in lessons.Where(l => key(l) is not null).GroupBy(l => (key(l)!.Value, l.Day)))
            {
                var ordered = group.OrderBy(Start).ToList();
                for (var i = 1; i < ordered.Count; i++)
                {
                    if (Start(ordered[i]) < End(ordered[i - 1]))
                        errors.Add($"{what} con dos clases a la vez el {group.Key.Day} a las {Start(ordered[i])}");
                }
            }
        }

        NoOverlap<CourseId>("Curso", l => l.CourseId);
        NoOverlap<TeacherId>("Docente", l => l.TeacherId);
        NoOverlap<Domain.Spaces.SpaceId>("Espacio", l => l.SpaceId);

        // 3) Intensidad horaria exacta por curso y asignatura, y nada fuera del plan
        foreach (var course in school.Courses)
        {
            var items = school.Plan.ItemsForGrade(course.GradeId);
            var courseLessons = lessons.Where(l => l.CourseId == course.Id).ToList();

            foreach (var extra in courseLessons.Where(l => items.All(i => i.SubjectId != l.SubjectId || i.DeliveryMode == DeliveryMode.Transversal)))
                errors.Add($"{Name(course)}: clase de {subjects[extra.SubjectId].Subject.Name} que no está en el plan (o es transversal)");

            foreach (var item in items.Where(i => i.DeliveryMode != DeliveryMode.Transversal))
            {
                var itemLessons = courseLessons.Where(l => l.SubjectId == item.SubjectId).ToList();
                var expected = item.HoursFor(timetable.AcademicPeriodId);
                var name = $"{Name(course)} · {subjects[item.SubjectId].Subject.Name}";

                if (expectComplete ? itemLessons.Count != expected : itemLessons.Count > expected)
                    errors.Add($"{name}: {itemLessons.Count} horas, plan {expected}");

                var expectedShift = item.DeliveryMode == DeliveryMode.CounterShift ? item.TargetShiftId!.Value : course.ShiftId;
                if (itemLessons.Any(l => l.ShiftId != expectedShift))
                    errors.Add($"{name}: clases fuera de la jornada que corresponde");

                foreach (var day in itemLessons.GroupBy(l => l.Day))
                {
                    var periods = day.Select(l => l.PeriodNumber).OrderBy(p => p).ToList();
                    if (item.MaxHoursPerDay is { } max && periods.Count > max)
                        errors.Add($"{name}: {periods.Count} horas el {day.Key} (máximo {max})");

                    var run = 1;
                    for (var i = 1; i < periods.Count; i++)
                    {
                        run = periods[i] == periods[i - 1] + 1 ? run + 1 : 1;
                        if (item.MaxConsecutiveHours is { } k && run > k)
                            errors.Add($"{name}: {run} horas seguidas el {day.Key} (máximo {k})");
                    }

                    if (item.MaxHoursPerDay == 2 && item.MaxConsecutiveHours >= 2 && periods.Count == 2 && periods[1] != periods[0] + 1)
                        errors.Add($"{name}: bloque doble partido el {day.Key}");
                }

                // Espacio: el tipo requerido, el salón del curso o ninguno en contrajornada
                foreach (var l in itemLessons)
                {
                    if (item.RequiredSpaceType is { } type && school.Spaces.Any(s => s.Type == type))
                    {
                        if (l.SpaceId is not { } sid || spaces[sid].Type != type)
                            errors.Add($"{name}: debía dictarse en un espacio de tipo {type}");
                    }
                    else if (item.DeliveryMode == DeliveryMode.CounterShift)
                    {
                        if (l.SpaceId is not null)
                            errors.Add($"{name}: en contrajornada no debe ocupar el salón del curso");
                    }
                    else if (l.SpaceId != course.HomeRoomId)
                    {
                        errors.Add($"{name}: debía dictarse en el salón del curso");
                    }
                }
            }

            // Carga diaria pareja en la jornada del curso (solo si el horario está completo)
            if (expectComplete)
            {
                var total = items.Where(i => i.DeliveryMode == DeliveryMode.Regular).Sum(i => i.HoursFor(timetable.AcademicPeriodId));
                var shift = school.Campus.FindShift(course.ShiftId)!;
                var days = shift.Days.ToDaysOfWeek();
                var (lo, hi) = (total / days.Count, (total + days.Count - 1) / days.Count);
                foreach (var d in days)
                {
                    var n = courseLessons.Count(l => l.Day == d && l.ShiftId == course.ShiftId);
                    if (n < lo || n > hi)
                        errors.Add($"{Name(course)}: {n} horas el {d} (esperado entre {lo} y {hi})");
                }
            }
        }

        // 4) Docentes: área, sede, carga, máximo diario, disponibilidad, otras sedes y coherencia con la asignación
        foreach (var group in lessons.Where(l => l.TeacherId is not null).GroupBy(l => l.TeacherId!.Value))
        {
            if (!teachers.TryGetValue(group.Key, out var teacher))
            {
                errors.Add($"Docente desconocido {group.Key}");
                continue;
            }

            var others = otherCampusLessons?.Count(l => l.TeacherId == teacher.Id) ?? 0;
            if (group.Count() + others > teacher.MaxWeeklyHours)
                errors.Add($"{teacher.FullName}: {group.Count() + others} horas semanales (máximo {teacher.MaxWeeklyHours})");

            foreach (var day in group.GroupBy(l => l.Day))
            {
                if (teacher.MaxDailyHours is { } maxDaily && day.Count() > maxDaily)
                    errors.Add($"{teacher.FullName}: {day.Count()} horas el {day.Key} (máximo {maxDaily})");
            }

            foreach (var l in group)
            {
                var manual = assignments.Any(a => a.CourseId == l.CourseId && a.SubjectId == l.SubjectId && a.IsManual && a.TeacherId == teacher.Id);
                if (!manual && !teacher.CanTeach(subjects[l.SubjectId].Area.Id))
                    errors.Add($"{teacher.FullName} dicta {subjects[l.SubjectId].Subject.Name} sin tener el área");
                if (!teacher.WorksAt(school.Campus.Id))
                    errors.Add($"{teacher.FullName} no trabaja en la sede");
                if (!teacher.IsAvailable(l.Day, Start(l), End(l)))
                    errors.Add($"{teacher.FullName} tiene clase en una franja en la que no está disponible ({l.Day} {Start(l)})");

                var assignment = assignments.FirstOrDefault(a => a.CourseId == l.CourseId && a.SubjectId == l.SubjectId);
                if (assignment is null || assignment.TeacherId != l.TeacherId)
                    errors.Add($"{teacher.FullName}: la clase no coincide con la asignación académica guardada");

                foreach (var o in otherCampusLessons?.Where(o => o.TeacherId == teacher.Id && o.Day == l.Day) ?? [])
                {
                    var otherBlock = school.OtherCampus.FindShift(o.ShiftId)!.FindBellSchedule(school.TypeA.Id)!.ClassPeriod(o.PeriodNumber)!;
                    if (otherBlock.Start < End(l) && Start(l) < otherBlock.End)
                        errors.Add($"{teacher.FullName} tiene clase en la otra sede a la misma hora ({l.Day} {Start(l)})");
                }
            }
        }

        return errors;
    }

    public static void ShouldBeStrictlyValid(
        this Timetable timetable,
        TestSchool school,
        IReadOnlyList<TeachingAssignment> assignments,
        IReadOnlyList<Lesson>? otherCampusLessons = null,
        bool expectComplete = true)
    {
        var errors = Validate(school, timetable, assignments, otherCampusLessons, expectComplete);
        errors.Should().BeEmpty("el horario debe cumplir todas las reglas, pero:\n" + string.Join("\n", errors));
    }
}
