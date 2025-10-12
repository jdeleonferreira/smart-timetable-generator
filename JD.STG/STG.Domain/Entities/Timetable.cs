using STG.Domain.Entities.Base;

namespace STG.Domain.Entities;

/// <summary>
/// Weekly timetable for a specific Group in a SchoolYear (e.g., 7A - 2025).
/// Aggregate root of <see cref="TimetableEntry"/>.
/// </summary>
/// <remarks>
/// Invariants:
/// - Unique per (GroupId, SchoolYearId).
/// - Entries cannot overlap on (DayOfWeek, PeriodIndex) within the same timetable.
/// - Each entry must reference an Assignment that belongs to the same (Group, SchoolYear).
/// </remarks>
public sealed class Timetable : Entity
{
    private readonly List<TimetableEntry> _entries = new();

    /// <summary>FK to the Group this timetable belongs to.</summary>
    public Guid GroupId { get; private set; }
    public Group Group { get; private set; } = null!;

    /// <summary>FK to the SchoolYear.</summary>
    public Guid SchoolYearId { get; private set; }
    public SchoolYear SchoolYear { get; private set; } = null!;

    /// <summary>Display name (e.g., "7A - 2025").</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Optional notes.</summary>
    public string? Notes { get; private set; }

    private Timetable() { } // EF

    public Timetable(Guid id, Guid groupId, Guid schoolYearId, string name)
    {
        Id = id == default ? Guid.NewGuid() : id;
        if (groupId == Guid.Empty) throw new ArgumentException("GroupId cannot be empty.", nameof(groupId));
        if (schoolYearId == Guid.Empty) throw new ArgumentException("SchoolYearId cannot be empty.", nameof(schoolYearId));
        Rename(name);

        GroupId = groupId;
        SchoolYearId = schoolYearId;
    }


    /// <summary>Entries (timeslots) for this timetable.</summary>
    public IReadOnlyCollection<TimetableEntry> Entries => _entries;

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty.", nameof(name));
        Name = name.Trim();
    }

    public void SetNotes(string? notes) => Notes = string.IsNullOrWhiteSpace(notes) ? null : notes!.Trim();
}
