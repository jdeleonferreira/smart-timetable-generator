using STG.Domain.Entities.Base;

namespace STG.Domain.Entities;

/// <summary>
/// Represents a class division within a specific grade of a school year.
/// Example: Grade "6" may contain groups "6A" and "6B".
/// </summary>
public sealed class Group : Entity
{
    /// <summary>
    /// Reference to the parent <see cref="Grade"/> entity.
    /// </summary>
    public Guid GradeId { get; private set; }

    /// <summary>
    /// Optional human-friendly grade name for quick access (denormalized).
    /// Example: "6" or "10".
    /// </summary>
    public string GradeName { get; private set; } = string.Empty;

    /// <summary>
    /// Group label (e.g., "A", "B").
    /// </summary>
    public string Label { get; private set; } = string.Empty;

    /// <summary>
    /// Number of enrolled students.
    /// </summary>
    public ushort Size { get; private set; }

    /// <summary>
    /// Concatenated group code (e.g., "6A", "10B").
    /// </summary>
    public string Code => $"{GradeName}{Label}";

    public const int MaxNameLength = 5; // e.g., "10A"

    private Group() { } // EF Core

    /// <summary>
    /// Creates a new group for the specified grade.
    /// </summary>
    /// <param name="gradeId">Identifier of the parent <see cref="Grade"/>.</param>
    /// <param name="gradeName">Readable grade label (e.g. "6").</param>
    /// <param name="label">Letter label (e.g. "A", "B").</param>
    /// <param name="size">Number of students in the group.</param>
    public Group(Guid gradeId, string gradeName, string label, ushort size)
    {
        if (gradeId == Guid.Empty) throw new ArgumentException("GradeId is required.", nameof(gradeId));
        if (string.IsNullOrWhiteSpace(gradeName)) throw new ArgumentException("Grade name is required.", nameof(gradeName));
        if (string.IsNullOrWhiteSpace(label)) label = "A";
        if (size == 0) throw new ArgumentOutOfRangeException(nameof(size), "Group size must be greater than zero.");

        SetCreated();
        Id = Guid.NewGuid();
        GradeId = gradeId;
        GradeName = gradeName.Trim();
        Label = label.Trim().ToUpperInvariant();
        Size = size;
    }

    /// <summary>
    /// Updates the number of students.
    /// </summary>
    public void Resize(ushort newSize, string? modifiedBy = null)
    {
        if (newSize == 0) throw new ArgumentOutOfRangeException(nameof(newSize));
        Size = newSize;
        SetModified(modifiedBy);
    }

    /// <summary>
    /// Changes the label (e.g., "A" → "B").
    /// </summary>
    public void Relabel(string newLabel, string? modifiedBy = null)
    {
        if (string.IsNullOrWhiteSpace(newLabel))
            throw new ArgumentException("Label is required.", nameof(newLabel));

        Label = newLabel.Trim().ToUpperInvariant();
        SetModified(modifiedBy);
    }

    public override string ToString() => $"{Code} ({Size} students)";
}
