using STG.Domain.Entities.Base;

namespace STG.Domain.Entities;

/// <summary>
/// Academic year aggregate root (e.g., 2025).
/// Domain rules:
/// 1) Year is a 4-digit integer within a reasonable range (e.g., 2000..2100 .. 3000).
/// 2) One SchoolYear per year value (persistence unique).
/// </summary>
public sealed class SchoolYear : Entity
{
    public int Year { get; private set; }

    private SchoolYear() { } // EF

    public SchoolYear(int year)
    {
        if (year < 2000 || year > 3000) throw new ArgumentOutOfRangeException(nameof(year), "Year must be between 2000 and 2100.");
        Id = Guid.NewGuid();
        Year = year;
        SetCreated();
    }

    public void SetYear(int newYear)
    {
        if (newYear < 2000 || newYear > 3000) throw new ArgumentOutOfRangeException(nameof(newYear), "Year must be between 2000 and 2100.");
        Year = newYear;
        SetModified();
    }

    public override string ToString() => Year.ToString();
}