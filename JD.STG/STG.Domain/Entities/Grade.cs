using STG.Domain.Entities.Base;

namespace STG.Domain.Entities;

/// <summary>
/// Represents an academic grade (e.g., 1st, 6th, 10th).
/// Belongs to a <see cref="SchoolYear"/> and is the parent of multiple groups (class divisions).
/// </summary>
public sealed class Grade : Entity
{
    /// <summary>
    /// The academic year this grade belongs to.
    /// </summary>
    public Guid SchoolYearId { get; private set; }

    /// <summary>
    /// Human-friendly name (e.g., "1st", "Grade 6", "Year 10").
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Optional numeric ordering for sorting (e.g., 1..11).
    /// </summary>
    public byte Order { get; private set; }

    private Grade() { }

    /// <summary>
    /// Creates a new grade within the specified school year.
    /// </summary>
    /// <param name="schoolYearId">Aggregate root identifier for <see cref="SchoolYear"/>.</param>
    /// <param name="name">Display name for the grade.</param>
    /// <param name="order">Optional sort order (1..255). Use 0 if you don't care.</param>
    public Grade(Guid schoolYearId, string name, byte order = 0)
    {
        if (schoolYearId == Guid.Empty) throw new ArgumentException("SchoolYearId is required.", nameof(schoolYearId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));

        SetCreated();
        Id = Guid.NewGuid();
        SchoolYearId = schoolYearId;
        Name = name.Trim();
        Order = order;
    }

    /// <summary>
    /// Renames the grade.
    /// </summary>
    public void Rename(string newName, string? modifiedBy = null)
    {
        if (string.IsNullOrWhiteSpace(newName)) throw new ArgumentException("Name is required.", nameof(newName));
        Name = newName.Trim();
        SetModified(modifiedBy);
    }

    /// <summary>
    /// Sets/updates the sortable numeric order. Use 0 to unset.
    /// </summary>
    public void SetOrder(byte order, string? modifiedBy = null)
    {
        Order = order;
        SetModified(modifiedBy);
    }

    public override string ToString() => $"{Name} (Order: {Order})";
}
