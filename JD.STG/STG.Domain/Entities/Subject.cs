using STG.Domain.Entities.Base;

namespace STG.Domain.Entities;

/// <summary>
/// Represents an academic subject taught in the school curriculum.
/// Example: Mathematics, English, Biology, Physics.
/// </summary>
public sealed class Subject : Entity
{
    /// <summary>
    /// Human-friendly subject name.
    /// Example: "Mathematics", "Biology".
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Indicates whether the subject requires laboratory resources.
    /// Example: true for Physics or Chemistry.
    /// </summary>
    public bool RequiresLab { get; private set; }

    /// <summary>
    /// Indicates whether the subject must be scheduled in double consecutive periods.
    /// Example: true for lab-based subjects or extended classes.
    /// </summary>
    public bool RequiresDoublePeriod { get; private set; }

    private Subject() { } // EF Core constructor

    /// <summary>
    /// Creates a new subject definition.
    /// </summary>
    /// <param name="name">Subject name.</param>
    /// <param name="requiresLab">Whether a lab is required.</param>
    /// <param name="requiresDoublePeriod">Whether it must occupy consecutive periods.</param>
    public Subject(string name, bool requiresLab = false, bool requiresDoublePeriod = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Subject name is required.", nameof(name));

        SetCreated();
        Id = Guid.NewGuid();

        Name = name.Trim();
        RequiresLab = requiresLab;
        RequiresDoublePeriod = requiresDoublePeriod;
    }

    /// <summary>
    /// Renames the subject.
    /// </summary>
    public void Rename(string newName, string? modifiedBy = null)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Subject name is required.", nameof(newName));

        Name = newName.Trim();
        SetModified(modifiedBy);
    }

    /// <summary>
    /// Updates the subject configuration.
    /// </summary>
    public void UpdateProperties(bool requiresLab, bool requiresDoublePeriod, string? modifiedBy = null)
    {
        RequiresLab = requiresLab;
        RequiresDoublePeriod = requiresDoublePeriod;
        SetModified(modifiedBy);
    }

    public override string ToString() =>
        $"{Name} (Lab: {RequiresLab}, Double: {RequiresDoublePeriod})";
}
