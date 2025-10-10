using STG.Domain.Entities.Base;

namespace STG.Domain.Entities;

/// <summary>
/// Represents a study plan entry (planned weekly hours per grade and subject).
/// Serves as a reference for validating teaching assignments (IH consistency).
/// </summary>
public sealed class StudyPlanEntry : Entity
{
    public Guid SchoolYearId { get; private set; }
    public Guid GradeId { get; private set; }
    public Guid SubjectId { get; private set; }

    /// <summary>
    /// Planned weekly instruction hours (IH) for this grade–subject combination.
    /// </summary>
    public byte WeeklyHours { get; private set; }

    private StudyPlanEntry() { } // EF Core

    public StudyPlanEntry(Guid schoolYearId, Guid gradeId, Guid subjectId, byte weeklyHours)
    {
        if (schoolYearId == Guid.Empty) throw new ArgumentException("SchoolYearId is required.");
        if (gradeId == Guid.Empty) throw new ArgumentException("GradeId is required.");
        if (subjectId == Guid.Empty) throw new ArgumentException("SubjectId is required.");
        if (weeklyHours is 0 or > 10) throw new ArgumentOutOfRangeException(nameof(weeklyHours));

        SetCreated();
        Id = Guid.NewGuid();
        SchoolYearId = schoolYearId;
        GradeId = gradeId;
        SubjectId = subjectId;
        WeeklyHours = weeklyHours;
    }

    public override string ToString() =>
        $"StudyPlanEntry [Grade={GradeId}, Subject={SubjectId}, IH={WeeklyHours}]";
}
