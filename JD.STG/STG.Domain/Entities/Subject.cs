using STG.Domain.Entities.Base;

namespace STG.Domain.Entities;

/// <summary>
/// Represents a school subject (e.g., Algebra, Biology) within a <see cref="StudyArea"/>.
/// </summary>
/// <remarks>
/// Invariants:
/// - Name: required (non-empty), unique at persistence.
/// - StudyAreaId: required (cannot be Guid.Empty).
/// - Code: optional, unique if present.
/// </remarks>
public sealed class Subject : Entity
{
    public string Name { get; private set; } = null!;

    /// <summary>Optional short code (unique if present).</summary>
    public string? Code { get; private set; }

    /// <summary>Marks whether the subject is elective.</summary>
    public bool IsElective { get; private set; }

    /// <summary>FK to <see cref="StudyArea"/>.</summary>
    public Guid StudyAreaId { get; private set; }

    /// <summary>Navigation to owning StudyArea.</summary>
    public StudyArea StudyArea { get; private set; } = null!;

    private Subject() { } // EF

    public Subject(Guid id, string name, Guid studyAreaId, string? code = null, bool isElective = false)
    {
        Id = id == default ? Guid.NewGuid() : id;
        Rename(name);
        SetStudyArea(studyAreaId);
        Recode(code);
        SetElective(isElective);
    }

    /// <summary>Display name (unique, non-empty).</summary>

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Subject name cannot be empty.", nameof(name));
        Name = name.Trim();
    }

    public void Recode(string? code)
        => Code = string.IsNullOrWhiteSpace(code) ? null : code!.Trim();

    public void SetElective(bool elective) => IsElective = elective;

    /// <summary>Sets the owning StudyArea (FK).</summary>
    public void SetStudyArea(Guid studyAreaId)
    {
        if (studyAreaId == Guid.Empty)
            throw new ArgumentException("StudyAreaId cannot be empty.", nameof(studyAreaId));
        StudyAreaId = studyAreaId;
    }
}
