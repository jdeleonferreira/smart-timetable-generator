using SmartTimetableGenerator.Domain.AcademicYears;
using SmartTimetableGenerator.Domain.Areas;
using SmartTimetableGenerator.Domain.Courses;
using SmartTimetableGenerator.Domain.Teachers;

namespace SmartTimetableGenerator.Domain.TeachingAssignments;

[ValueObject<Guid>]
public readonly partial struct TeachingAssignmentId;

public enum AssignmentSource
{
    /// <summary>Asignado a mano por el coordinador. El generador no lo cambia.</summary>
    Manual = 0,

    /// <summary>Propuesto por el sistema según áreas y carga del docente.</summary>
    Suggested = 1
}

/// <summary>
/// Asignación académica: qué docente dicta una asignatura en un curso.
/// Si <see cref="AcademicPeriodId"/> es null aplica a todo el año; si no, solo a ese periodo
/// (permite cambiar de docente a mitad de año).
/// </summary>
public class TeachingAssignment : AggregateRoot<TeachingAssignmentId>
{
    public AcademicYearId AcademicYearId { get; private set; }
    public CourseId CourseId { get; private set; }
    public SubjectId SubjectId { get; private set; }
    public AcademicPeriodId? AcademicPeriodId { get; private set; }
    public TeacherId? TeacherId { get; private set; }
    public AssignmentSource Source { get; private set; }

    private TeachingAssignment() { } // Needed for EF Core

    public static TeachingAssignment Create(
        AcademicYearId academicYearId,
        CourseId courseId,
        SubjectId subjectId,
        AcademicPeriodId? academicPeriodId = null)
    {
        return new TeachingAssignment
        {
            Id = TeachingAssignmentId.From(Guid.CreateVersion7()),
            AcademicYearId = academicYearId,
            CourseId = courseId,
            SubjectId = subjectId,
            AcademicPeriodId = academicPeriodId,
            Source = AssignmentSource.Suggested
        };
    }

    public bool IsManual => Source == AssignmentSource.Manual;

    /// <summary>Asignación fijada por el coordinador.</summary>
    public void AssignManually(TeacherId teacherId)
    {
        TeacherId = teacherId;
        Source = AssignmentSource.Manual;
    }

    /// <summary>Propuesta del sistema. No sobrescribe una asignación manual.</summary>
    public ErrorOr<Success> Suggest(TeacherId? teacherId)
    {
        if (IsManual)
            return TeachingAssignmentErrors.ManualAssignmentLocked;

        TeacherId = teacherId;
        Source = AssignmentSource.Suggested;
        return Result.Success;
    }

    /// <summary>Libera la asignación para que el sistema pueda proponer otro docente.</summary>
    public void Release()
    {
        TeacherId = null;
        Source = AssignmentSource.Suggested;
    }
}
