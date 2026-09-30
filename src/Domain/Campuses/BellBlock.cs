namespace SmartTimetableGenerator.Domain.Campuses;

public enum BellBlockKind
{
    Class = 0,
    Break = 1
}

/// <summary>
/// Franja del horario de timbre: una hora de clase o un descanso.
/// </summary>
public record BellBlock : IValueObject
{
    public const int LabelMaxLength = 50;

    // Private setters needed for EF
    public BellBlockKind Kind { get; private set; }

    public TimeOnly Start { get; private set; }

    public TimeOnly End { get; private set; }

    public string? Label { get; private set; }

    /// <summary>Número de franja de clase (1..N). Null para descansos. Lo asigna el horario de timbre.</summary>
    public int? PeriodNumber { get; private set; }

    public BellBlock(BellBlockKind kind, TimeOnly start, TimeOnly end, string? label = null, int? periodNumber = null)
    {
        if (end <= start)
            throw new ArgumentOutOfRangeException(nameof(end), "La hora final debe ser posterior a la inicial");

        Kind = kind;
        Start = start;
        End = end;
        Label = TextRules.Optional(label, LabelMaxLength, nameof(label));
        PeriodNumber = kind == BellBlockKind.Class ? periodNumber : null;
    }

    public int DurationMinutes => (int)(End - Start).TotalMinutes;
}
