using STG.Domain.Entities.Base;

namespace STG.Domain.Entities;

/// <summary>
/// Scheduled cell for a specific (DayOfWeek, PeriodNumber) pointing to an Assignment.
/// Domain rules:
/// 1) Must belong to a Timetable (immutable) and target an Assignment (immutable).
/// 2) (TimetableId, DayOfWeek, PeriodNumber) must be unique (persistence).
/// 3) PeriodNumber is 1..20.
/// </summary>
public sealed class TimetableEntry : Entity
{
    public Guid TimetableId { get; private set; }

    public DayOfWeek DayOfWeek { get; private set; }
    public int PeriodNumber { get; private set; } // 1..20

    public Guid AssignmentId { get; private set; }
    public Guid AssignmentTeacherId { get; private set; } // denormalized for quick checks
    public Guid AssignmentSubjectId { get; private set; } // denormalized for reporting

    public Guid? RoomId { get; private set; } // optional
    public string? ReasonJson { get; private set; } // optional lightweight explanation

    private TimetableEntry() { } // EF

    public TimetableEntry(Guid timetableId, DayOfWeek dayOfWeek, int periodNumber,
                          Guid assignmentId, Guid assignmentTeacherId, Guid assignmentSubjectId,
                          Guid? roomId = null, string? reasonJson = null)
    {
        if (timetableId == Guid.Empty) throw new ArgumentException("TimetableId is required.", nameof(timetableId));
        if (assignmentId == Guid.Empty) throw new ArgumentException("AssignmentId is required.", nameof(assignmentId));
        if (assignmentTeacherId == Guid.Empty) throw new ArgumentException("AssignmentTeacherId is required.", nameof(assignmentTeacherId));
        if (assignmentSubjectId == Guid.Empty) throw new ArgumentException("AssignmentSubjectId is required.", nameof(assignmentSubjectId));
        if (periodNumber < 1 || periodNumber > 20) throw new ArgumentOutOfRangeException(nameof(periodNumber), "PeriodNumber must be between 1 and 20.");

        Id = Guid.NewGuid();
        TimetableId = timetableId;
        DayOfWeek = dayOfWeek;
        PeriodNumber = periodNumber;
        AssignmentId = assignmentId;
        AssignmentTeacherId = assignmentTeacherId;
        AssignmentSubjectId = assignmentSubjectId;
        RoomId = roomId;
        ReasonJson = string.IsNullOrWhiteSpace(reasonJson) ? null : reasonJson.Trim();
        SetCreated();
    }

    public TimetableEntry SetRoom(Guid? roomId, string? modifiedBy = null)
    {
        RoomId = roomId;
        SetModified(modifiedBy);
        return this;
    }

    public TimetableEntry SetReason(string? json, string? modifiedBy = null)
    {
        ReasonJson = string.IsNullOrWhiteSpace(json) ? null : json.Trim();
        SetModified(modifiedBy);
        return this;
    }

    public override string ToString() => $"{DayOfWeek} P{PeriodNumber} • A:{AssignmentId}";
}
