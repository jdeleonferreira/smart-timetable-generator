using STG.Domain.Entities.Base;

namespace STG.Domain.Entities;

/// <summary>
/// Canonical period definition for a (SchoolYear, DayOfWeek, PeriodNumber) cell.
/// Domain rules:
/// 1) (SchoolYearId, DayOfWeek, PeriodNumber) is unique.
/// 2) PeriodNumber is 1..20 (inclusive).
/// 3) If both times are provided, StartTime &lt; EndTime.
/// 4) When <see cref="IsBreak"/> is true, an optional <see cref="Label"/> can be set.
/// </summary>
public sealed class PeriodSlot : Entity
{
    public const int MaxLabelLength = 64;

    public Guid SchoolYearId { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }
    public int PeriodNumber { get; private set; } // 1-based

    public TimeSpan? StartTime { get; private set; }
    public TimeSpan? EndTime { get; private set; }

    public bool IsBreak { get; private set; }
    public string? Label { get; private set; }

    private PeriodSlot() { } // EF

    /// <summary>Factory constructor that enforces invariants.</summary>
    public PeriodSlot(
        Guid schoolYearId,
        DayOfWeek dayOfWeek,
        int periodNumber,
        TimeSpan? startTime = null,
        TimeSpan? endTime = null,
        bool isBreak = false,
        string? label = null)
    {
        if (schoolYearId == Guid.Empty) throw new ArgumentException("SchoolYearId is required.", nameof(schoolYearId));
        if (periodNumber < 1 || periodNumber > 20) throw new ArgumentOutOfRangeException(nameof(periodNumber), "PeriodNumber must be between 1 and 20.");
        if (label is { Length: > MaxLabelLength }) throw new ArgumentException($"Label must be <= {MaxLabelLength} chars.", nameof(label));
        if (startTime.HasValue && endTime.HasValue && startTime.Value >= endTime.Value)
            throw new ArgumentException("StartTime must be earlier than EndTime when both are provided.");

        Id = Guid.NewGuid();
        SchoolYearId = schoolYearId;
        DayOfWeek = dayOfWeek;
        PeriodNumber = periodNumber;
        StartTime = startTime;
        EndTime = endTime;
        IsBreak = isBreak;
        Label = NormalizeLabel(label);
        SetCreated();
    }

    /// <summary>Sets or clears the explicit time range.</summary>
    public PeriodSlot SetTime(TimeSpan? start, TimeSpan? end, string? modifiedBy = null)
    {
        if (start.HasValue && end.HasValue && start.Value >= end.Value)
            throw new ArgumentException("StartTime must be earlier than EndTime when both are provided.");
        StartTime = start;
        EndTime = end;
        SetModified(modifiedBy);
        return this;
    }

    /// <summary>Marks this slot as a break and optionally sets a label (e.g., "Recess").</summary>
    public PeriodSlot MarkBreak(string? label = null, string? modifiedBy = null)
    {
        if (label is { Length: > MaxLabelLength }) throw new ArgumentException($"Label must be <= {MaxLabelLength} chars.", nameof(label));
        IsBreak = true;
        Label = NormalizeLabel(label) ?? Label ?? "Break";
        SetModified(modifiedBy);
        return this;
    }

    /// <summary>Clears the break flag.</summary>
    public PeriodSlot UnmarkBreak(string? modifiedBy = null)
    {
        IsBreak = false;
        SetModified(modifiedBy);
        return this;
    }

    /// <summary>Changes the label regardless of break state.</summary>
    public PeriodSlot RenameLabel(string? label, string? modifiedBy = null)
    {
        if (label is { Length: > MaxLabelLength }) throw new ArgumentException($"Label must be <= {MaxLabelLength} chars.", nameof(label));
        Label = NormalizeLabel(label);
        SetModified(modifiedBy);
        return this;
    }

    private static string? NormalizeLabel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var t = value.Trim();
        while (t.Contains("  ")) t = t.Replace("  ", " ");
        return t;
    }

    public override string ToString() => $"{DayOfWeek} P{PeriodNumber} {(IsBreak ? "[Break]" : string.Empty)}".Trim();
}
