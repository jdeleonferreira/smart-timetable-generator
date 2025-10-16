namespace STG.Domain.ValueObjects;

/// <summary>
/// Represents the structural configuration of a school week.
/// Defines which days are active, how many periods (blocks) each day contains,
/// and how long each period lasts.
/// </summary>
public sealed class WeekConfig
{
    /// <summary>
    /// Ordered list of active teaching days in the week.
    /// Example: Monday–Friday.
    /// </summary>
    public IReadOnlyList<DayOfWeek> Days { get; }

    /// <summary>
    /// Number of class periods (blocks) per day.
    /// Example: 8 blocks per day.
    /// </summary>
    public byte BlocksPerDay { get; }

    /// <summary>
    /// Duration of each block in minutes.
    /// Example: 45-minute periods.
    /// </summary>
    public ushort BlockLengthMinutes { get; }

    private WeekConfig() { } // Required for EF Core materialization

    public WeekConfig(IEnumerable<DayOfWeek> days, byte blocksPerDay, ushort blockLengthMinutes)
    {
        if (days is null)
            throw new ArgumentNullException(nameof(days));

        var list = days.Distinct().OrderBy(d => d).ToList();
        if (list.Count == 0)
            throw new ArgumentException("Week must include at least one day.", nameof(days));
        if (blocksPerDay == 0)
            throw new ArgumentOutOfRangeException(nameof(blocksPerDay), "BlocksPerDay must be greater than zero.");
        if (blockLengthMinutes == 0)
            throw new ArgumentOutOfRangeException(nameof(blockLengthMinutes), "BlockLengthMinutes must be greater than zero.");

        Days = list.AsReadOnly();
        BlocksPerDay = blocksPerDay;
        BlockLengthMinutes = blockLengthMinutes;
    }

    /// <summary>
    /// Determines whether the specified time slot is valid within this week configuration.
    /// </summary>
    /// <param name="slot">The time slot to check.</param>
    /// <returns>True if the slot's day and block fit within the configured week.</returns>
    public bool IsValidSlot(TimeSlot slot)
    {
        if (slot is null) return false;
        return Days.Contains(slot.Day) &&
               slot.Block >= 1 &&
               slot.Block <= BlocksPerDay;
    }

    /// <summary>
    /// Returns a readable summary of the week configuration.
    /// </summary>
    public override string ToString()
    {
        var days = string.Join(", ", Days);
        return $"{days} | {BlocksPerDay} blocks/day @ {BlockLengthMinutes} min";
    }
}
