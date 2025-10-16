namespace STG.Domain.ValueObjects;

/// <summary>
/// Represents a position in the timetable grid (Day + Period number).
/// This is a Value Object: equality is determined by its content, not by reference.
/// </summary>
public sealed class TimeSlot : IEquatable<TimeSlot>
{
    /// <summary>
    /// Day of the week (e.g. Monday, Tuesday...).
    /// </summary>
    public DayOfWeek Day { get; }

    /// <summary>
    /// Period number within the day (1..12 typically).
    /// </summary>
    public byte Block { get; }

    private TimeSlot() { } // Required for EF Core materialization

    public TimeSlot(DayOfWeek day, byte block)
    {
        if (block is 0 or > 12)
            throw new ArgumentOutOfRangeException(nameof(block), "Block must be between 1 and 12.");

        Day = day;
        Block = block;
    }

    /// <summary>
    /// Factory method for cleaner construction.
    /// </summary>
    public static TimeSlot Of(DayOfWeek day, byte block) => new(day, block);

    // ---------------------------
    // Value Equality
    // ---------------------------
    public bool Equals(TimeSlot? other)
    {
        if (other is null) return false;
        return Day == other.Day && Block == other.Block;
    }

    public override bool Equals(object? obj) => Equals(obj as TimeSlot);

    public override int GetHashCode() => HashCode.Combine((int)Day, Block);

    public static bool operator ==(TimeSlot? a, TimeSlot? b) => a?.Equals(b) ?? b is null;
    public static bool operator !=(TimeSlot? a, TimeSlot? b) => !(a == b);

    public override string ToString() => $"{Day} P{Block}";
}
