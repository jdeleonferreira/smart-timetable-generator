using STG.Domain.Entities.Base;
using STG.Domain.ValueObjects;

namespace STG.Domain.Entities;

/// <summary>
/// A single scheduled unit (one period) that places an Assignment
/// at a specific (day, period) within the timetable grid.
/// </summary>
public sealed class TimetableEntry : Entity
{
    /// <summary>Parent timetable.</summary>
    public Guid TimetableId { get; private set; }

    /// <summary>Academic year (must match the parent timetable's SchoolYearId).</summary>
    public Guid SchoolYearId { get; private set; }

    /// <summary>The assignment being placed (demand source).</summary>
    public Guid AssignmentId { get; private set; }

    /// <summary>Redundant denormalized references for fast checks and indexing.</summary>
    public Guid GroupId { get; private set; }
    public Guid SubjectId { get; private set; }
    public Guid TeacherId { get; private set; }

    /// <summary>Placement in the week grid.</summary>
    public TimeSlot Slot { get; private set; }

    private TimetableEntry() { } // EF Core

    /// <summary>
    /// Creates a new scheduled entry (one period) for an assignment.
    /// </summary>
    public TimetableEntry(
        Guid timetableId,
        Guid schoolYearId,
        Guid assignmentId,
        Guid groupId,
        Guid subjectId,
        Guid teacherId,
        TimeSlot slot)
    {
        if (timetableId == Guid.Empty) throw new ArgumentException("TimetableId is required.", nameof(timetableId));
        if (schoolYearId == Guid.Empty) throw new ArgumentException("SchoolYearId is required.", nameof(schoolYearId));
        if (assignmentId == Guid.Empty) throw new ArgumentException("AssignmentId is required.", nameof(assignmentId));
        if (groupId == Guid.Empty) throw new ArgumentException("GroupId is required.", nameof(groupId));
        if (subjectId == Guid.Empty) throw new ArgumentException("SubjectId is required.", nameof(subjectId));
        if (teacherId == Guid.Empty) throw new ArgumentException("TeacherId is required.", nameof(teacherId));
        if (slot is null) throw new ArgumentNullException(nameof(slot));

        SetCreated();
        Id = Guid.NewGuid();

        TimetableId = timetableId;
        SchoolYearId = schoolYearId;
        AssignmentId = assignmentId;
        GroupId = groupId;
        SubjectId = subjectId;
        TeacherId = teacherId;
        Slot = slot;
    }

    public override string ToString() =>
        $"Entry[{Id}] A={AssignmentId} G={GroupId} T={TeacherId} S={SubjectId} @ {Slot}";
}
