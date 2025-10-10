using STG.Domain.Entities.Base;

namespace STG.Domain.Entities;

/// <summary>
/// Study plan line that ties a <see cref="Subject"/> to weekly hours (IH) within a <see cref="StudyPlan"/>.
/// Domain rules:
/// 1) Must belong to a StudyPlan (immutable).
/// 2) Must reference a Subject (immutable).
/// 3) WeeklyHours is 1..50 (reasonable guardrail).
/// 4) (StudyPlanId, SubjectId) must be unique (enforced in persistence).
/// </summary>
public sealed class StudyPlanEntry : Entity
{
    public Guid StudyPlanId { get; private set; }
    public Guid SubjectId { get; private set; }

    public int WeeklyHours { get; private set; } // IH per week

    private StudyPlanEntry() { } // EF

    /// <summary>Factory constructor that enforces invariants.</summary>
    public StudyPlanEntry(Guid studyPlanId, Guid subjectId, int weeklyHours)
    {
        if (studyPlanId == Guid.Empty) throw new ArgumentException("StudyPlanId is required.", nameof(studyPlanId));
        if (subjectId == Guid.Empty) throw new ArgumentException("SubjectId is required.", nameof(subjectId));
        if (weeklyHours < 1 || weeklyHours > 50) throw new ArgumentOutOfRangeException(nameof(weeklyHours), "WeeklyHours must be between 1 and 50.");

        Id = Guid.NewGuid();
        StudyPlanId = studyPlanId;
        SubjectId = subjectId;
        WeeklyHours = weeklyHours;
        SetCreated();
    }

    /// <summary>Updates weekly hours (1..50).</summary>
    public StudyPlanEntry SetWeeklyHours(int weeklyHours, string? modifiedBy = null)
    {
        if (weeklyHours < 1 || weeklyHours > 50) throw new ArgumentOutOfRangeException(nameof(weeklyHours), "WeeklyHours must be between 1 and 50.");
        WeeklyHours = weeklyHours;
        SetModified(modifiedBy);
        return this;
    }

    public override string ToString() => $"Subject:{SubjectId} IH:{WeeklyHours}";
}
