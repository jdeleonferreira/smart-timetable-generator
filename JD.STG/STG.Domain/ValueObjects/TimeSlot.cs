using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STG.Domain.ValueObjects;

public readonly record struct TimeSlot(DayOfWeek Day, int Block)
{
    public static TimeSlot Of(DayOfWeek day, int block)
    {
        if (block < 1) throw new ArgumentOutOfRangeException(nameof(block), "Block must be >= 1");
        return new TimeSlot(day, block);
    }
}
