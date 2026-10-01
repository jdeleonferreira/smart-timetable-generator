using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Domain.AcademicYears;
using SmartTimetableGenerator.Domain.Areas;
using SmartTimetableGenerator.Domain.Campuses;
using SmartTimetableGenerator.Domain.Common.Calendar;
using SmartTimetableGenerator.Domain.Courses;
using SmartTimetableGenerator.Domain.DayTypes;
using SmartTimetableGenerator.Domain.Grades;
using SmartTimetableGenerator.Domain.Spaces;
using SmartTimetableGenerator.Domain.StudyPlans;
using SmartTimetableGenerator.Domain.Teachers;
using SmartTimetableGenerator.Domain.Timetables;

namespace SmartTimetableGenerator.Scheduling.Tests.Common;

/// <summary>
/// Colegio pequeño y conocido para las pruebas de generación:
/// sede principal con jornada mañana (L–V, 6 franjas) y tarde (L–V, 4 franjas), otra sede, grados 6º y 7º
/// con cursos A y B (22 h semanales cada uno), 6 docentes y una sala de informática.
/// Cada prueba modifica lo que necesita antes de guardar.
/// </summary>
public sealed class TestSchool
{
    public DayType TypeA { get; }
    public Campus Campus { get; }
    public Shift Morning { get; }
    public Shift Afternoon { get; }
    public Campus OtherCampus { get; }
    public Shift OtherMorning { get; }
    public AcademicYear Year { get; }
    public AcademicPeriod Period1 { get; }
    public AcademicPeriod Period2 { get; }
    public Grade Sixth { get; }
    public Grade Seventh { get; }

    public Area Math { get; }
    public Area Humanities { get; }
    public Area Science { get; }
    public Area Technology { get; }
    public Area Sports { get; }
    public Area Ethics { get; }

    public Subject Arithmetic { get; }
    public Subject Spanish { get; }
    public Subject English { get; }
    public Subject Biology { get; }
    public Subject Informatics { get; }
    public Subject PhysicalEducation { get; }
    public Subject Research { get; }
    public Subject Citizenship { get; }

    public StudyPlan Plan { get; }
    public List<Course> Courses { get; } = [];
    public List<Space> Spaces { get; } = [];
    public Space ComputerLab { get; }

    public Teacher MathTeacher { get; }
    public Teacher HumanitiesTeacher1 { get; }
    public Teacher HumanitiesTeacher2 { get; }
    public Teacher ScienceTeacher { get; }
    public Teacher TechTeacher { get; }
    public Teacher SportsTeacher { get; }
    public List<Teacher> Teachers { get; }

    public Timetable Timetable { get; }

    public Course C6A => Courses[0];
    public Course C6B => Courses[1];
    public Course C7A => Courses[2];
    public Course C7B => Courses[3];

    /// <summary>Horas semanales regulares por curso en el plan base.</summary>
    public const int BaseWeeklyHours = 22;

    public const int MorningPeriods = 6;

    public TestSchool()
    {
        TypeA = DayType.Create("A", "Normal", 50, isDefault: true);

        Campus = Campus.Create("Sede Principal");
        Morning = Campus.AddShift("Mañana", SchoolDays.MondayToFriday).Value;
        Afternoon = Campus.AddShift("Tarde", SchoolDays.MondayToFriday).Value;
        Campus.SetBellSchedule(Morning.Id, TypeA.Id,
        [
            Class(7, 0, 7, 50), Class(7, 50, 8, 40), Class(8, 40, 9, 30),
            new BellBlock(BellBlockKind.Break, new TimeOnly(9, 30), new TimeOnly(10, 0), "Descanso"),
            Class(10, 0, 10, 50), Class(10, 50, 11, 40), Class(11, 40, 12, 30)
        ]);
        Campus.SetBellSchedule(Afternoon.Id, TypeA.Id,
        [
            Class(13, 0, 13, 50), Class(13, 50, 14, 40), Class(14, 40, 15, 30), Class(15, 30, 16, 20)
        ]);

        OtherCampus = Campus.Create("Sede Norte");
        OtherMorning = OtherCampus.AddShift("Mañana", SchoolDays.MondayToFriday).Value;
        OtherCampus.SetBellSchedule(OtherMorning.Id, TypeA.Id,
        [
            Class(7, 0, 7, 50), Class(7, 50, 8, 40), Class(8, 40, 9, 30), Class(10, 0, 10, 50)
        ]);

        Year = AcademicYear.Create(2026, new DateOnly(2026, 1, 26), new DateOnly(2026, 11, 27));
        Period1 = Year.AddPeriod("Periodo 1", new DateOnly(2026, 1, 26), new DateOnly(2026, 6, 19)).Value;
        Period2 = Year.AddPeriod("Periodo 2", new DateOnly(2026, 7, 13), new DateOnly(2026, 11, 27)).Value;

        Sixth = Grade.Create("Sexto", "6º", EducationLevel.LowerSecondary, 6);
        Seventh = Grade.Create("Séptimo", "7º", EducationLevel.LowerSecondary, 7);

        Math = Area.Create("Matemáticas", 0);
        Arithmetic = Math.AddSubject("Aritmética", "ARI").Value;
        Humanities = Area.Create("Humanidades", 1);
        Spanish = Humanities.AddSubject("Lengua castellana", "LEN").Value;
        English = Humanities.AddSubject("Inglés", "ING").Value;
        Science = Area.Create("Ciencias Naturales", 2);
        Biology = Science.AddSubject("Biología", "BIO").Value;
        Research = Science.AddSubject("Proyecto de investigación", "PIN").Value;
        Technology = Area.Create("Tecnología e Informática", 3);
        Informatics = Technology.AddSubject("Informática", "INF").Value;
        Sports = Area.Create("Educación Física", 4);
        PhysicalEducation = Sports.AddSubject("Educación física", "EDF").Value;
        Ethics = Area.Create("Ciencias Sociales", 5);
        Citizenship = Ethics.AddSubject("Competencia ciudadana", "CCI").Value;

        Plan = StudyPlan.Create(Year.Id, Campus.Id, "Plan de prueba");
        foreach (var grade in new[] { Sixth, Seventh })
        {
            AddRegular(grade, Arithmetic, 5, 2, 2);
            AddRegular(grade, Spanish, 5, 2, 2);
            AddRegular(grade, English, 4, 2, 2);
            AddRegular(grade, Biology, 4, 2, 2);
            AddRegular(grade, Informatics, 2, 1, 1);
            AddRegular(grade, PhysicalEducation, 2, 2, 2);
        }

        ComputerLab = Space.Create(Campus.Id, "Sala de informática", SpaceType.ComputerLab, 40);
        Spaces.Add(ComputerLab);

        var room = 101;
        foreach (var grade in new[] { Sixth, Seventh })
        {
            foreach (var name in new[] { "A", "B" })
            {
                var space = Space.Create(Campus.Id, $"Salón {room++}", SpaceType.Classroom, 40);
                Spaces.Add(space);
                var course = Course.Create(Year.Id, Campus.Id, Morning.Id, grade.Id, name);
                course.AssignHomeRoom(space.Id);
                Courses.Add(course);
            }
        }

        MathTeacher = NewTeacher("Ana", "Matemáticas", Math);
        HumanitiesTeacher1 = NewTeacher("Beatriz", "Lengua", Humanities);
        HumanitiesTeacher2 = NewTeacher("Carlos", "Inglés", Humanities);
        ScienceTeacher = NewTeacher("Diana", "Ciencias", Science);
        TechTeacher = NewTeacher("Esteban", "Informática", Technology);
        SportsTeacher = NewTeacher("Fabio", "Deportes", Sports);
        Teachers = [MathTeacher, HumanitiesTeacher1, HumanitiesTeacher2, ScienceTeacher, TechTeacher, SportsTeacher];

        Timetable = Timetable.Create(Year.Id, Campus.Id, Period1.Id, Plan.Id, "Horario de prueba");
    }

    public StudyPlanItem Item(Grade grade, Subject subject) => Plan.FindItem(grade.Id, subject.Id)!;

    public IEnumerable<Course> CoursesOf(Grade grade) => Courses.Where(c => c.GradeId == grade.Id);

    public void AddRegular(Grade grade, Subject subject, int hours, int maxPerDay, int maxConsecutive)
    {
        var item = Plan.AddItem(grade.Id, subject.Id, DeliveryMode.Regular, hours).Value;
        Plan.SetItemDistribution(item.Id, maxPerDay, maxConsecutive, null).IsError.Should().BeFalse();
    }

    public void Seed(IApplicationDbContext db)
    {
        db.DayTypes.Add(TypeA);
        db.Campuses.AddRange(Campus, OtherCampus);
        db.AcademicYears.Add(Year);
        db.Grades.AddRange(Sixth, Seventh);
        db.Areas.AddRange(Math, Humanities, Science, Technology, Sports, Ethics);
        db.StudyPlans.Add(Plan);
        db.Spaces.AddRange(Spaces);
        db.Courses.AddRange(Courses);
        db.Teachers.AddRange(Teachers);
        db.Timetables.Add(Timetable);
    }

    private Teacher NewTeacher(string firstName, string lastName, Area area)
    {
        var teacher = Teacher.Create(firstName, lastName, 22, $"{firstName.ToLowerInvariant()}@colegio.test");
        teacher.SetWorkload(22, 6, null).IsError.Should().BeFalse();
        teacher.SetAreas([area.Id]);
        teacher.SetCampuses([Campus.Id]);
        return teacher;
    }

    private static BellBlock Class(int h1, int m1, int h2, int m2) =>
        new(BellBlockKind.Class, new TimeOnly(h1, m1), new TimeOnly(h2, m2));
}
