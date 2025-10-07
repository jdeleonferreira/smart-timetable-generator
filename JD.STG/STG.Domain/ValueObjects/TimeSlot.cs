namespace STG.Domain.ValueObjects;

public class TimeSlot
{
    public DayOfWeek Day { get; private set; }
    public int Block { get; private set; }

    private TimeSlot() { }

    public TimeSlot(DayOfWeek day, int block)
    {
        if (block < 1) throw new ArgumentOutOfRangeException(nameof(block), "Block must be >= 1");
        Day = day;
        Block = block;
    }

    public static TimeSlot Of(DayOfWeek day, int block) => new(day, block);
}
