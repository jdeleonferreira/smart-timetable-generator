using STG.Domain.Entities.Base;

namespace STG.Domain.Entities;

/// <summary>
/// Teacher availability window in period units for a specific day.
/// Domain rules:
/// 1) Must belong to a teacher (immutable reference).
/// 2) PeriodFrom/To are 1..20 and From &lt;= To.
/// 3) <see cref="IsAvailable"/> indicates availability (true) or a block (false).
/// </summary>
public sealed class AvailabilityBlock : Entity
{
    public Guid TeacherId { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }
    public int PeriodFrom { get; private set; }
    public int PeriodTo { get; private set; }
    public bool IsAvailable { get; private set; }

    private AvailabilityBlock() { } // EF

    /// <summary>Factory constructor that enforces invariants.</summary>
    public AvailabilityBlock(Guid teacherId, DayOfWeek dayOfWeek, int periodFrom, int periodTo, bool isAvailable = true)
    {
        if (teacherId == Guid.Empty) throw new ArgumentException("TeacherId is required.", nameof(teacherId));
        if (periodFrom < 1 || periodFrom > 20) throw new ArgumentOutOfRangeException(nameof(periodFrom));
        if (periodTo < 1 || periodTo > 20) throw new ArgumentOutOfRangeException(nameof(periodTo));
        if (periodFrom > periodTo) throw new ArgumentException("PeriodFrom must be <= PeriodTo.");

        Id = Guid.NewGuid();
        TeacherId = teacherId;
        DayOfWeek = dayOfWeek;
        PeriodFrom = periodFrom;
        PeriodTo = periodTo;
        IsAvailable = isAvailable;
        SetCreated();
    }

    /// <summary>Switches this block between available/unavailable.</summary>
    public AvailabilityBlock SetAvailability(bool available, string? modifiedBy = null)
    {
        IsAvailable = available;
        SetModified(modifiedBy);
        return this;
    }
}
