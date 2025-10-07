using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STG.Domain.ValueObjects;

public sealed class WeekConfig
{
    public IReadOnlyList<DayOfWeek> Days { get; }
    public int BlocksPerDay { get; }
    public int BlockLengthMinutes { get; }

    WeekConfig() { }

    public WeekConfig(IEnumerable<DayOfWeek> days, int blocksPerDay, int blockLengthMinutes)
    {
        var list = days?.Distinct().OrderBy(d => d).ToList() ?? throw new ArgumentNullException(nameof(days));
        if (!list.Any()) throw new ArgumentException("Week must have at least one day.");
        if (blocksPerDay <= 0) throw new ArgumentOutOfRangeException(nameof(blocksPerDay));
        if (blockLengthMinutes <= 0) throw new ArgumentOutOfRangeException(nameof(blockLengthMinutes));

        Days = list;
        BlocksPerDay = blocksPerDay;
        BlockLengthMinutes = blockLengthMinutes;
    }

    public bool IsValidSlot(TimeSlot slot) =>
        Days.Contains(slot.Day) && (slot.Block >= 1 && slot.Block <= BlocksPerDay);
}

