using SmartTimetableGenerator.Domain.Areas;
using SmartTimetableGenerator.Domain.Campuses;
using SmartTimetableGenerator.Domain.Courses;
using SmartTimetableGenerator.Domain.Spaces;
using SmartTimetableGenerator.Domain.Teachers;

namespace SmartTimetableGenerator.Domain.Timetables;

[ValueObject<Guid>]
public readonly partial struct LessonId;

/// <summary>
/// Una hora de clase en el horario: curso + asignatura + docente + espacio en (jornada, día, franja).
/// </summary>
public class Lesson : Entity<LessonId>
{
    public CourseId CourseId { get; private set; }
    public SubjectId SubjectId { get; private set; }
    public TeacherId? TeacherId { get; private set; }
    public SpaceId? SpaceId { get; private set; }
    public ShiftId ShiftId { get; private set; }
    public DayOfWeek Day { get; private set; }

    /// <summary>Número de franja de clase (1..N) según el horario de timbre de la jornada.</summary>
    public int PeriodNumber { get; private set; }

    /// <summary>Fijada a mano: el generador no la mueve.</summary>
    public bool IsLocked { get; private set; }

    public LessonPlacement Placement => new(CourseId, SubjectId, TeacherId, SpaceId, ShiftId, Day, PeriodNumber);

    private Lesson() { } // Needed for EF Core

    internal static Lesson Create(LessonPlacement placement, bool isLocked)
    {
        var lesson = new Lesson { Id = LessonId.From(Guid.CreateVersion7()), IsLocked = isLocked };
        lesson.MoveTo(placement);
        return lesson;
    }

    internal void MoveTo(LessonPlacement placement)
    {
        ThrowIfNull(placement);
        ThrowIfLessThan(placement.PeriodNumber, 1, nameof(placement));

        CourseId = placement.CourseId;
        SubjectId = placement.SubjectId;
        TeacherId = placement.TeacherId;
        SpaceId = placement.SpaceId;
        ShiftId = placement.ShiftId;
        Day = placement.Day;
        PeriodNumber = placement.PeriodNumber;
    }

    internal void SetLocked(bool isLocked) => IsLocked = isLocked;
}

/// <summary>
/// Ubicación de una clase en el horario (entrada/salida del generador).
/// </summary>
public sealed record LessonPlacement(
    CourseId CourseId,
    SubjectId SubjectId,
    TeacherId? TeacherId,
    SpaceId? SpaceId,
    ShiftId ShiftId,
    DayOfWeek Day,
    int PeriodNumber) : IValueObject;
