using SmartTimetableGenerator.Domain.AcademicYears;
using SmartTimetableGenerator.Domain.Campuses;
using SmartTimetableGenerator.Domain.Grades;
using SmartTimetableGenerator.Domain.Spaces;
using SmartTimetableGenerator.Domain.Teachers;

namespace SmartTimetableGenerator.Domain.Courses;

[ValueObject<Guid>]
public readonly partial struct CourseId;

/// <summary>
/// Curso (grupo) de un grado en un año lectivo, sede y jornada. Ej.: 6ºA.
/// Al iniciar el año se le asigna su salón y su director de grupo.
/// </summary>
public class Course : AggregateRoot<CourseId>
{
    public const int NameMaxLength = 20;

    public AcademicYearId AcademicYearId { get; private set; }
    public CampusId CampusId { get; private set; }
    public ShiftId ShiftId { get; private set; }
    public GradeId GradeId { get; private set; }

    /// <summary>Identificador del grupo dentro del grado (ej.: "A", "01").</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Salón asignado al curso para el año.</summary>
    public SpaceId? HomeRoomId { get; private set; }

    /// <summary>Director de grupo.</summary>
    public TeacherId? HomeroomTeacherId { get; private set; }

    public int? StudentCount { get; private set; }

    private Course() { } // Needed for EF Core

    public static Course Create(AcademicYearId academicYearId, CampusId campusId, ShiftId shiftId, GradeId gradeId, string name)
    {
        var course = new Course
        {
            Id = CourseId.From(Guid.CreateVersion7()),
            AcademicYearId = academicYearId,
            CampusId = campusId,
            GradeId = gradeId
        };

        course.Update(shiftId, name, null);
        return course;
    }

    public void Update(ShiftId shiftId, string name, int? studentCount)
    {
        if (studentCount is not null)
            ThrowIfNegative(studentCount.Value, nameof(studentCount));

        ShiftId = shiftId;
        Name = TextRules.Required(name, NameMaxLength, nameof(name)).ToUpperInvariant();
        StudentCount = studentCount;
    }

    public void AssignHomeRoom(SpaceId? spaceId) => HomeRoomId = spaceId;

    public void AssignHomeroomTeacher(TeacherId? teacherId) => HomeroomTeacherId = teacherId;
}
