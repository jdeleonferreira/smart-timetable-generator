using STG.Domain.Entities.Base;

namespace STG.Domain.Entities;

/// <summary>
/// Represents a school grade/level (e.g., Preschool, 1st, … 11th).
/// </summary>
/// <remarks>
/// Invariants:
/// - Name: required, unique.
/// - Order: required, unique (used for sorting; e.g., 0=Preschool, 1..11).
/// </remarks>
public sealed class Grade : Entity 
{
    private Grade() { } // EF

    /// <summary>Display name (unique, non-empty).</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Ordering number for UI/reports (unique).</summary>
    public byte Order { get; private set; }

    public Grade(Guid id, string name, byte order)
    {
        Id = id == default ? Guid.NewGuid() : id;
        Rename(name);
        Reorder(order);
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Grade name cannot be empty.", nameof(name));
        Name = name.Trim();
    }

    public void Reorder(byte order) => Order = order;
}
