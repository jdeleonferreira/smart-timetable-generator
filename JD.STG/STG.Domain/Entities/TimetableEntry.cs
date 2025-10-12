using STG.Domain.Entities.Base;

namespace STG.Domain.Entities;

/// <summary>
/// Single timeslot within a weekly timetable.
/// Links to an <see cref="Assignment"/> and occupies (DayOfWeek, PeriodIndex).
/// </summary>
/// <remarks>
/// Invariants:
/// - DayOfWeek in [0..6] (0=Sunday or set your convention).
/// - PeriodIndex in [1..24].
/// - Span in [1..8] (consecutive periods; use 1 if your blocks are unitary).
/// </remarks>
public sealed class TimetableEntry : Entity
{
    public Guid TimetableId { get; private set; }
    public Timetable Timetable { get; private set; } = null!;

    /// <summary>Linked assignment (Group, Subject, Year) that is being scheduled.</summary>
    public Guid AssignmentId { get; private set; }
    public Assignment Assignment { get; private set; } = null!;

    /// <summary>0..6, choose your convention (e.g., 1=Mon..5=Fri).</summary>
    public byte DayOfWeek { get; private set; }

    /// <summary>1..24 fixed index in the day. Use bell table externally if needed.</summary>
    public byte PeriodIndex { get; private set; }

    /// <summary>Number of consecutive periods (>=1).</summary>
    public byte Span { get; private set; }

    /// <summary>Optional room/code.</summary>
    public string? Room { get; private set; }

    /// <summary>Optional notes.</summary>
    public string? Notes { get; private set; }

    private TimetableEntry() { } // EF

    public TimetableEntry(Guid id, Guid timetableId, Guid assignmentId, byte dayOfWeek, byte periodIndex, byte span = 1, string? room = null, string? notes = null)
    {
        Id = id == default ? Guid.NewGuid() : id;

        if (timetableId == Guid.Empty) throw new ArgumentException("TimetableId cannot be empty.", nameof(timetableId));
        if (assignmentId == Guid.Empty) throw new ArgumentException("AssignmentId cannot be empty.", nameof(assignmentId));
        SetDayAndPeriod(dayOfWeek, periodIndex, span);

        TimetableId = timetableId;
        AssignmentId = assignmentId;
        SetRoom(room);
        SetNotes(notes);
    }


    public void SetDayAndPeriod(byte dayOfWeek, byte periodIndex, byte span = 1)
    {
        if (dayOfWeek > 6) throw new ArgumentOutOfRangeException(nameof(dayOfWeek), "DayOfWeek must be between 0 and 6.");
        if (periodIndex is < 1 or > 24) throw new ArgumentOutOfRangeException(nameof(periodIndex), "PeriodIndex must be between 1 and 24.");
        if (span is < 1 or > 8) throw new ArgumentOutOfRangeException(nameof(span), "Span must be between 1 and 8.");
        DayOfWeek = dayOfWeek;
        PeriodIndex = periodIndex;
        Span = span;
    }

    public void SetRoom(string? room) => Room = string.IsNullOrWhiteSpace(room) ? null : room.Trim();
    public void SetNotes(string? notes) => Notes = string.IsNullOrWhiteSpace(notes) ? null : notes!.Trim();
}