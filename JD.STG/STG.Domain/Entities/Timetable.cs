using STG.Domain.Entities.Base;
using STG.Domain.ValueObjects;

namespace STG.Domain.Entities;

/// <summary>
/// Aggregate root representing the scheduled timetable for a school year.
/// It holds period placements for assignments and enforces hard constraints:
/// - No group can have two classes in the same (day, period).
/// - No teacher can teach two classes in the same (day, period).
/// </summary>
public sealed class Timetable : AggregateRoot
{
    private readonly List<TimetableEntry> _entries = new();

    /// <summary>
    /// Academic year this timetable belongs to.
    /// </summary>
    public Guid SchoolYearId { get; private set; }

    /// <summary>
    /// Read-only view of all scheduled entries.
    /// </summary>
    public IReadOnlyCollection<TimetableEntry> Entries => _entries.AsReadOnly();

    private Timetable() : base(createdBy: null) { } // EF Core

    /// <summary>
    /// Creates a new timetable for the specified school year.
    /// </summary>
    public Timetable(Guid schoolYearId, string? createdBy = null) : base(createdBy)
    {
        if (schoolYearId == Guid.Empty)
            throw new ArgumentException("SchoolYearId is required.", nameof(schoolYearId));

        Id = Guid.NewGuid();
        SchoolYearId = schoolYearId;
    }

    /// <summary>
    /// Attempts to add a new timetable entry, enforcing hard constraints.
    /// Throws <see cref="InvalidOperationException"/> if any rule is violated.
    /// </summary>
    public void AddEntry(TimetableEntry entry, string? modifiedBy = null)
    {
        if (entry is null) throw new ArgumentNullException(nameof(entry));
        if (entry.SchoolYearId != SchoolYearId)
            throw new InvalidOperationException("Entry belongs to a different SchoolYear.");

        // 1) Group overlap: one group per (day, period)
        if (_entries.Any(e =>
            e.GroupId == entry.GroupId &&
            e.Slot.Day == entry.Slot.Day &&
            e.Slot.Block == entry.Slot.Block))
        {
            throw new InvalidOperationException("Group period already assigned.");
        }

        // 2) Teacher overlap: one teacher per (day, period)
        if (_entries.Any(e =>
            e.TeacherId == entry.TeacherId &&
            e.Slot.Day == entry.Slot.Day &&
            e.Slot.Block == entry.Slot.Block))
        {
            throw new InvalidOperationException("Teacher overlap.");
        }

        _entries.Add(entry);
        MarkModified(modifiedBy);
    }

    /// <summary>
    /// Removes an entry from the timetable, if present.
    /// </summary>
    public bool RemoveEntry(Guid entryId, string? modifiedBy = null)
    {
        var removed = _entries.RemoveAll(e => e.Id == entryId) > 0;
        if (removed) MarkModified(modifiedBy);
        return removed;
    }

    /// <summary>
    /// Checks if a given slot is already taken by either the group or the teacher.
    /// Returns a short diagnostic message if blocked; otherwise null.
    /// </summary>
    public string? GetBlockingReason(Guid groupId, Guid teacherId, TimeSlot slot)
    {
        if (_entries.Any(e => e.GroupId == groupId && e.Slot == slot))
            return "Group period already assigned.";
        if (_entries.Any(e => e.TeacherId == teacherId && e.Slot == slot))
            return "Teacher overlap.";
        return null;
    }

    /// <summary>
    /// Clears all scheduled entries. Use with care (admin operation).
    /// </summary>
    public void Clear(string? modifiedBy = null)
    {
        if (_entries.Count == 0) return;
        _entries.Clear();
        MarkModified(modifiedBy);
    }

    public override string ToString() => $"Timetable[{SchoolYearId}] Entries={_entries.Count}";
}
