// FILE: STG.Domain/Entities/SchoolYear.cs
using STG.Domain.Entities.Base;

namespace STG.Domain.Entities;

/// <summary>
/// Academic year aggregate root (e.g., 2025).
/// Domain rules:
/// 1) Year is a 4-digit integer within a reasonable range (e.g., 2000..2100).
/// 2) One SchoolYear per year value (persistence unique).
/// </summary>
public sealed class SchoolYear : Entity
{
    public int Year { get; private set; }

    private SchoolYear() { } // EF

    public SchoolYear(int year)
    {
        if (year < 2000 || year > 2100) throw new ArgumentOutOfRangeException(nameof(year), "Year must be between 2000 and 2100.");
        Id = Guid.NewGuid();
        Year = year;
        SetCreated();
    }

    public override string ToString() => Year.ToString();
}