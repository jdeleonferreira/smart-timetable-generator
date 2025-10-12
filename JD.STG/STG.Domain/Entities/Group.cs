// src/STG.Domain/Entities/Group.cs
using STG.Domain.Entities.Base;

namespace STG.Domain.Entities;

/// <summary>
/// Represents a class division within a Grade (e.g., 7A, 7B).
/// </summary>
/// <remarks>
/// Invariants:
/// - Name: required (non-empty).
/// - (GradeId, Name) must be unique at persistence level.
/// </remarks>
public sealed class Group : Entity
{
    /// <summary>FK to <see cref="Grade"/> the group belongs to.</summary>
    public Guid GradeId { get; private set; }

    /// <summary>Navigation to parent Grade.</summary>
    public Grade Grade { get; private set; } = null!;

    /// <summary>Short label (e.g., "A", "B", "7A"). Unique per grade.</summary>
    public string Name { get; private set; } = null!;

    private Group() { } // EF

    public Group(Guid id, Guid gradeId, string name)
    {
        Id = id == default ? Guid.NewGuid() : id;
        SetGrade(gradeId);
        Rename(name);
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Group name cannot be empty.", nameof(name));
        Name = name.Trim();
    }

    public void SetGrade(Guid gradeId)
    {
        if (gradeId == Guid.Empty)
            throw new ArgumentException("GradeId cannot be empty.", nameof(gradeId));
        GradeId = gradeId;
    }
}