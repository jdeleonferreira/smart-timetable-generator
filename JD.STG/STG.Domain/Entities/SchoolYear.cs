using STG.Domain.Entities.Base;
using STG.Domain.ValueObjects;

namespace STG.Domain.Entities;

/// <summary>
/// Aggregate root representing an academic year. 
/// Acts as the version boundary for configuration and data such as grades, groups, 
/// study plan (IH), timetable, and scheduling settings.
/// </summary>
public sealed class SchoolYear : AggregateRoot
{
    /// <summary>
    /// Calendar year (e.g., 2025).
    /// </summary>
    public short Year { get; private set; }

    /// <summary>
    /// Week structure (active days, blocks per day, block length).
    /// </summary>
    public WeekConfig Week { get; private set; }

    /// <summary>
    /// Indicates whether the year is open for changes (seed, assignments, scheduling).
    /// </summary>
    public bool IsOpen { get; private set; }

    // EF Core needs a parameterless constructor
    private SchoolYear() : base(createdBy: null) { }

    /// <summary>
    /// Creates a new academic year aggregate with the provided week configuration.
    /// </summary>
    public SchoolYear(short year, WeekConfig week, string? createdBy = null) : base(createdBy)
    {
        if (year < 2000 || year > 2100)
            throw new ArgumentOutOfRangeException(nameof(year), "Year must be between 2000 and 2100.");
        Week = week ?? throw new ArgumentNullException(nameof(week));

        Year = year;
        IsOpen = true;
    }

    /// <summary>
    /// Updates the week configuration (days, periods per day, block length).
    /// </summary>
    public void UpdateWeek(WeekConfig newWeek, string? modifiedBy = null)
    {
        if (!IsOpen) throw new InvalidOperationException("SchoolYear is closed.");
        Week = newWeek ?? throw new ArgumentNullException(nameof(newWeek));
        MarkModified(modifiedBy);
    }

    /// <summary>
    /// Closes the year for further structural changes (useful after seeding + timetable approval).
    /// </summary>
    public void Close(string? modifiedBy = null)
    {
        if (!IsOpen) return;
        IsOpen = false;
        MarkModified(modifiedBy);
    }

    /// <summary>
    /// Reopens the year if needed (admin override).
    /// </summary>
    public void Reopen(string? modifiedBy = null)
    {
        if (IsOpen) return;
        IsOpen = true;
        MarkModified(modifiedBy);
    }

    public override string ToString() => $"SchoolYear {Year} (Open: {IsOpen})";
}
