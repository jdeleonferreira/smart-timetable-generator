using STG.Domain.Entities.Base;

namespace STG.Domain.Entities;

/// <summary>
/// Teaching assignment that binds a Group, Subject and Teacher for a SchoolYear,
/// carrying the weekly instructional hours (IH) the scheduler must place.
/// Domain rules:
/// 1) Must belong to a SchoolYear and reference Group, Subject, Teacher (immutables).
/// 2) WeeklyHours is 1..50.
/// 3) (SchoolYearId, GroupId, SubjectId, TeacherId) should be unique (persistence).
/// </summary>
public sealed class Assignment : Entity
{
    public Guid SchoolYearId { get; private set; }
    public Guid GroupId { get; private set; }
    public Guid SubjectId { get; private set; }
    public Guid TeacherId { get; private set; }

    public int WeeklyHours { get; private set; }

    private Assignment() { } // EF

    public Assignment(Guid schoolYearId, Guid groupId, Guid subjectId, Guid teacherId, int weeklyHours)
    {
        if (schoolYearId == Guid.Empty) throw new ArgumentException("SchoolYearId is required.", nameof(schoolYearId));
        if (groupId == Guid.Empty) throw new ArgumentException("GroupId is required.", nameof(groupId));
        if (subjectId == Guid.Empty) throw new ArgumentException("SubjectId is required.", nameof(subjectId));
        if (teacherId == Guid.Empty) throw new ArgumentException("TeacherId is required.", nameof(teacherId));
        if (weeklyHours < 1 || weeklyHours > 50) throw new ArgumentOutOfRangeException(nameof(weeklyHours), "WeeklyHours must be between 1 and 50.");

        Id = Guid.NewGuid();
        SchoolYearId = schoolYearId;
        GroupId = groupId;
        SubjectId = subjectId;
        TeacherId = teacherId;
        WeeklyHours = weeklyHours;
        SetCreated();
    }

    /// <summary>Updates weekly hours (1..50).</summary>
    public Assignment SetWeeklyHours(int weeklyHours, string? modifiedBy = null)
    {
        if (weeklyHours < 1 || weeklyHours > 50) throw new ArgumentOutOfRangeException(nameof(weeklyHours), "WeeklyHours must be between 1 and 50.");
        WeeklyHours = weeklyHours;
        SetModified(modifiedBy);
        return this;
    }

    public override string ToString() => $"G:{GroupId} S:{SubjectId} T:{TeacherId} IH:{WeeklyHours}";
}
