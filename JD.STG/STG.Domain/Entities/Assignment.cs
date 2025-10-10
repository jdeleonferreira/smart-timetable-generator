using STG.Domain.Entities.Base;

namespace STG.Domain.Entities;

/// <summary>
/// Teaching demand for a given school year:
/// which teacher delivers which subject to which group, and for how many weekly hours (IH).
/// Note: It does NOT contain timetable placement; that lives in Timetable/TimetableEntry.
/// </summary>
public sealed class Assignment : Entity
{
    /// <summary>Academic year aggregate this assignment belongs to (e.g., 2025).</summary>
    public Guid SchoolYearId { get; private set; }

    /// <summary>Target group (e.g., 6A, 7B...).</summary>
    public Guid GroupId { get; private set; }

    /// <summary>Subject to be taught (e.g., Mathematics, English...).</summary>
    public Guid SubjectId { get; private set; }

    /// <summary>Assigned teacher.</summary>
    public Guid TeacherId { get; private set; }

    /// <summary>Weekly instruction hours (IH). Typical range: 1..10.</summary>
    public byte WeeklyHours { get; private set; }

    // EF Core parameterless constructor
    private Assignment() { }

    /// <summary>
    /// Creates a new teaching assignment (demand) for a school year.
    /// </summary>
    public Assignment(Guid schoolYearId, Guid groupId, Guid subjectId, Guid teacherId, byte weeklyHours)
    {
        if (schoolYearId == Guid.Empty) throw new ArgumentException("SchoolYearId is required.", nameof(schoolYearId));
        if (groupId == Guid.Empty) throw new ArgumentException("GroupId is required.", nameof(groupId));
        if (subjectId == Guid.Empty) throw new ArgumentException("SubjectId is required.", nameof(subjectId));
        if (teacherId == Guid.Empty) throw new ArgumentException("TeacherId is required.", nameof(teacherId));
        if (weeklyHours is 0 or > 10) throw new ArgumentOutOfRangeException(nameof(weeklyHours), "WeeklyHours must be between 1 and 10.");

        SetCreated();
        Id = Guid.NewGuid();

        SchoolYearId = schoolYearId;
        GroupId = groupId;
        SubjectId = subjectId;
        TeacherId = teacherId;
        WeeklyHours = weeklyHours;
    }

    /// <summary>
    /// Reassigns the teacher for this assignment.
    /// </summary>
    public void ChangeTeacher(Guid newTeacherId, string? modifiedBy = null)
    {
        if (newTeacherId == Guid.Empty) throw new ArgumentException("TeacherId is required.", nameof(newTeacherId));
        TeacherId = newTeacherId;
        SetModified(modifiedBy);
    }

    /// <summary>
    /// Updates the weekly instruction hours (IH).
    /// </summary>
    public void ChangeWeeklyHours(byte newWeeklyHours, string? modifiedBy = null)
    {
        if (newWeeklyHours is 0 or > 10) throw new ArgumentOutOfRangeException(nameof(newWeeklyHours), "WeeklyHours must be between 1 and 10.");
        WeeklyHours = newWeeklyHours;
        SetModified(modifiedBy);
    }

    public override string ToString() =>
        $"Assignment [Year={SchoolYearId}, Group={GroupId}, Subject={SubjectId}, Teacher={TeacherId}, IH={WeeklyHours}]";
}
