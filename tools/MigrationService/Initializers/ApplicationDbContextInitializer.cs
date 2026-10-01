using Bogus;
using Microsoft.EntityFrameworkCore;
using SmartTimetableGenerator.Domain.AcademicYears;
using SmartTimetableGenerator.Domain.Areas;
using SmartTimetableGenerator.Domain.Campuses;
using SmartTimetableGenerator.Domain.Common.Calendar;
using SmartTimetableGenerator.Domain.Courses;
using SmartTimetableGenerator.Domain.DayTypes;
using SmartTimetableGenerator.Domain.Grades;
using SmartTimetableGenerator.Domain.Institutions;
using SmartTimetableGenerator.Domain.Spaces;
using SmartTimetableGenerator.Domain.StudyPlans;
using SmartTimetableGenerator.Domain.Teachers;
using SmartTimetableGenerator.Domain.TrainingProjects;
using SmartTimetableGenerator.Infrastructure.Persistence;

namespace MigrationService.Initializers;

/// <summary>
/// Carga datos de ejemplo (solo en Development): una sede con jornada mañana, tipos de jornada A y B,
/// el plan de estudios del documento, dos cursos por grado con su salón, docentes ficticios y proyectos de formación.
/// </summary>
public class ApplicationDbContextInitializer(ApplicationDbContext dbContext) : DbContextInitializerBase<ApplicationDbContext>(dbContext)
{
    private const int SampleYear = 2026;
    private const int TeacherWeeklyHours = 22;
    private static readonly string[] CourseNames = ["A", "B"];

    public async Task SeedDataAsync(CancellationToken cancellationToken)
    {
        if (await DbContext.Institutions.AnyAsync(cancellationToken))
            return;

        var strategy = DbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);

            DbContext.Institutions.Add(Institution.Create("Institución Educativa de Ejemplo"));

            var (dayTypeA, dayTypeB) = SeedDayTypes();
            var (campus, morning) = SeedCampus(dayTypeA, dayTypeB);
            var grades = SeedGrades();
            var areas = SeedAreas();
            var year = SeedAcademicYear();
            SeedStudyPlan(year, campus, grades, areas);
            var teachers = SeedTeachers(campus, areas);
            SeedCoursesAndRooms(year, campus, morning, grades, teachers);
            SeedTrainingProjects(year);

            await DbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }

    private (DayType A, DayType B) SeedDayTypes()
    {
        var a = DayType.Create("A", "Jornada normal", 45, isDefault: true, "Horas de clase de 45 minutos");
        var b = DayType.Create("B", "Jornada con proyectos", 40,
            description: "Horas de clase de 40 minutos; el tiempo restante se usa en los proyectos de formación");

        DbContext.DayTypes.AddRange(a, b);
        return (a, b);
    }

    private (Campus Campus, Shift Morning) SeedCampus(DayType dayTypeA, DayType dayTypeB)
    {
        var campus = Campus.Create("Sede Principal", "SP");
        var morning = campus.AddShift("Mañana", SchoolDays.MondayToFriday).Value;

        // Tipo A: 8 horas de 45 minutos
        campus.SetBellSchedule(morning.Id, dayTypeA.Id,
        [
            Class(6, 30, 7, 15), Class(7, 15, 8, 0), Class(8, 0, 8, 45),
            Break(8, 45, 9, 15, "Descanso"),
            Class(9, 15, 10, 0), Class(10, 0, 10, 45), Class(10, 45, 11, 30),
            Break(11, 30, 11, 45, "Descanso"),
            Class(11, 45, 12, 30), Class(12, 30, 13, 15)
        ]);

        // Tipo B: 8 horas de 40 minutos; de 12:35 a 13:15 queda tiempo para proyectos
        campus.SetBellSchedule(morning.Id, dayTypeB.Id,
        [
            Class(6, 30, 7, 10), Class(7, 10, 7, 50), Class(7, 50, 8, 30),
            Break(8, 30, 9, 0, "Descanso"),
            Class(9, 0, 9, 40), Class(9, 40, 10, 20), Class(10, 20, 11, 0),
            Break(11, 0, 11, 15, "Descanso"),
            Class(11, 15, 11, 55), Class(11, 55, 12, 35),
            Break(12, 35, 13, 15, "Proyectos de formación")
        ]);

        DbContext.Campuses.Add(campus);
        return (campus, morning);

        static BellBlock Class(int h1, int m1, int h2, int m2) =>
            new(BellBlockKind.Class, new TimeOnly(h1, m1), new TimeOnly(h2, m2));

        static BellBlock Break(int h1, int m1, int h2, int m2, string label) =>
            new(BellBlockKind.Break, new TimeOnly(h1, m1), new TimeOnly(h2, m2), label);
    }

    private List<Grade> SeedGrades()
    {
        var grades = SampleStudyPlan.Grades
            .Select(g => Grade.Create(g.Name, g.ShortName, LevelFor(g.Order), g.Order))
            .ToList();

        DbContext.Grades.AddRange(grades);
        return grades;

        static EducationLevel LevelFor(int order) => order switch
        {
            0 => EducationLevel.Preschool,
            <= 5 => EducationLevel.Primary,
            <= 9 => EducationLevel.LowerSecondary,
            _ => EducationLevel.UpperSecondary
        };
    }

    private List<Area> SeedAreas()
    {
        var areas = new List<Area>();
        var order = 0;

        foreach (var row in SampleStudyPlan.Areas)
        {
            var area = Area.Create(row.Name, order++);
            foreach (var subject in row.Subjects)
                area.AddSubject(subject.Name, subject.Code);

            areas.Add(area);
        }

        DbContext.Areas.AddRange(areas);
        return areas;
    }

    private AcademicYear SeedAcademicYear()
    {
        var year = AcademicYear.Create(SampleYear, new DateOnly(SampleYear, 1, 26), new DateOnly(SampleYear, 11, 27));

        year.AddPeriod("Periodo 1", new DateOnly(SampleYear, 1, 26), new DateOnly(SampleYear, 4, 10));
        year.AddPeriod("Periodo 2", new DateOnly(SampleYear, 4, 13), new DateOnly(SampleYear, 6, 19));
        year.AddPeriod("Periodo 3", new DateOnly(SampleYear, 7, 13), new DateOnly(SampleYear, 9, 18));
        year.AddPeriod("Periodo 4", new DateOnly(SampleYear, 9, 21), new DateOnly(SampleYear, 11, 27));

        year.LoadColombianHolidays();
        year.AddNonSchoolDays(new DateOnly(SampleYear, 3, 30), new DateOnly(SampleYear, 4, 3), CalendarEntryKind.Recess, "Semana Santa");
        year.AddNonSchoolDays(new DateOnly(SampleYear, 6, 22), new DateOnly(SampleYear, 7, 10), CalendarEntryKind.Recess, "Vacaciones de mitad de año");
        year.AddNonSchoolDays(new DateOnly(SampleYear, 10, 5), new DateOnly(SampleYear, 10, 9), CalendarEntryKind.Recess, "Receso estudiantil de octubre");
        year.Activate();

        DbContext.AcademicYears.Add(year);
        return year;
    }

    private void SeedStudyPlan(AcademicYear year, Campus campus, List<Grade> grades, List<Area> areas)
    {
        var plan = StudyPlan.Create(year.Id, campus.Id, $"Plan de estudios {SampleYear} - {campus.Name}");
        plan.Update(plan.Name, SampleStudyPlan.PlanNotes);

        var subjectsByName = areas.SelectMany(a => a.Subjects).ToDictionary(s => s.Name);

        foreach (var row in SampleStudyPlan.Areas.SelectMany(a => a.Subjects))
        {
            var subject = subjectsByName[row.Name];
            SubjectId? integratedInto = row.IntegratedInto is null ? null : subjectsByName[row.IntegratedInto].Id;

            for (var i = 0; i < row.Hours.Length; i++)
            {
                if (row.Hours[i] is not { } hours)
                    continue;

                var mode = hours == SampleStudyPlan.T ? DeliveryMode.Transversal : DeliveryMode.Regular;
                var result = plan.AddItem(
                    grades[i].Id,
                    subject.Id,
                    mode,
                    mode == DeliveryMode.Transversal ? 0 : hours,
                    integratedIntoSubjectId: mode == DeliveryMode.Transversal ? integratedInto : null,
                    note: row.Note);

                if (result.IsError)
                    throw new InvalidOperationException($"{row.Name} / {grades[i].Name}: {result.FirstError.Description}");

                // Reglas por defecto: máximo 2 horas por día y bloques de hasta 2 horas seguidas
                if (mode == DeliveryMode.Regular)
                    plan.SetItemDistribution(result.Value.Id, maxHoursPerDay: Math.Min(2, hours), maxConsecutiveHours: Math.Min(2, hours), requiredSpaceType: null);
            }
        }

        for (var i = 0; i < grades.Count; i++)
        {
            var total = plan.WeeklyTotal(grades[i].Id);
            if (total != SampleStudyPlan.ExpectedWeeklyTotals[i])
                throw new InvalidOperationException($"Total semanal de {grades[i].Name}: {total}, esperado {SampleStudyPlan.ExpectedWeeklyTotals[i]}");
        }

        DbContext.StudyPlans.Add(plan);
    }

    /// <summary>
    /// Docentes ficticios: por cada área, los necesarios para cubrir sus horas (2 cursos por grado) con 22 h semanales.
    /// </summary>
    private List<Teacher> SeedTeachers(Campus campus, List<Area> areas)
    {
        var faker = new Faker("es");
        faker.Random = new Randomizer(2026);

        var teachers = new List<Teacher>();
        var areaRows = SampleStudyPlan.Areas;

        for (var a = 0; a < areas.Count; a++)
        {
            var weeklyHours = areaRows[a].Subjects
                .SelectMany(s => s.Hours)
                .Where(h => h is > 0)
                .Sum(h => h!.Value) * CourseNames.Length;

            var count = Math.Max(1, (int)Math.Ceiling(weeklyHours / (double)TeacherWeeklyHours));

            for (var i = 0; i < count; i++)
            {
                var firstName = faker.Name.FirstName();
                var lastName = faker.Name.LastName();
                var email = $"{Slug(firstName)}.{Slug(lastName)}{teachers.Count + 1}@colegio.example";

                var teacher = Teacher.Create(firstName, lastName, TeacherWeeklyHours, email);
                teacher.SetWorkload(TeacherWeeklyHours, maxDailyHours: 6, maxGapsPerDay: 2);
                teacher.SetAreas([areas[a].Id]);
                teacher.SetCampuses([campus.Id]);
                teachers.Add(teacher);
            }
        }

        DbContext.Teachers.AddRange(teachers);
        return teachers;

        static string Slug(string value) => new(value.ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => char.IsAsciiLetter(c)).ToArray());
    }

    private void SeedCoursesAndRooms(AcademicYear year, Campus campus, Shift morning, List<Grade> grades, List<Teacher> teachers)
    {
        var spaces = new List<Space>
        {
            Space.Create(campus.Id, "Sala de informática 1", SpaceType.ComputerLab, 40),
            Space.Create(campus.Id, "Biblioteca", SpaceType.Library, 50),
            Space.Create(campus.Id, "Laboratorio de ciencias", SpaceType.Laboratory, 40),
            Space.Create(campus.Id, "Cancha múltiple", SpaceType.SportsField)
        };

        var courses = new List<Course>();
        var roomNumber = 101;
        var directorIndex = 0;

        foreach (var grade in grades)
        {
            foreach (var name in CourseNames)
            {
                var room = Space.Create(campus.Id, $"Salón {roomNumber++}", SpaceType.Classroom, 40);
                spaces.Add(room);

                var course = Course.Create(year.Id, campus.Id, morning.Id, grade.Id, name);
                course.Update(morning.Id, name, studentCount: grade.Order == 0 ? 25 : 35);
                course.AssignHomeRoom(room.Id);
                course.AssignHomeroomTeacher(teachers[directorIndex++ % teachers.Count].Id);
                courses.Add(course);
            }
        }

        DbContext.Spaces.AddRange(spaces);
        DbContext.Courses.AddRange(courses);
    }

    private void SeedTrainingProjects(AcademicYear year)
    {
        var projects = SampleStudyPlan.TrainingProjects
            .Select(p => TrainingProject.Create(year.Id, p.Name, p.Mandatory ? ProjectCategory.Mandatory : ProjectCategory.Institutional))
            .ToList();

        DbContext.TrainingProjects.AddRange(projects);
    }
}
