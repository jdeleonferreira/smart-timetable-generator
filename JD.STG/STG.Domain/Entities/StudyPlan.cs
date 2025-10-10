using STG.Domain.Entities.Base;

namespace STG.Domain.Entities;

/// <summary>
/// Study plan header for a (SchoolYear, Grade) scope, e.g., "Plan 2011".
/// Domain rules:
/// 1) Must belong to exactly one school year and one grade (immutable).
/// 2) Name is required, trimmed, <= 128 chars; unique per (SchoolYearId, GradeId).
/// 3) Lines (Curriculum) are managed separately to avoid coupling here.
/// </summary>
public sealed class StudyPlan : Entity
{
    public const int MaxNameLength = 128;

    public Guid SchoolYearId { get; private set; }
    public Guid GradeId { get; private set; }

    public string Name { get; private set; } = "Plan";

    private StudyPlan() { } // EF

    /// <summary>Factory constructor that enforces invariants.</summary>
    public StudyPlan(Guid schoolYearId, Guid gradeId, string name = "Plan 2011")
    {
        if (schoolYearId == Guid.Empty) throw new ArgumentException("SchoolYearId is required.", nameof(schoolYearId));
        if (gradeId == Guid.Empty) throw new ArgumentException("GradeId is required.", nameof(gradeId));

        name = NormalizeName(name);
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (name.Length > MaxNameLength) throw new ArgumentException($"Name must be <= {MaxNameLength} chars.", nameof(name));

        Id = Guid.NewGuid();
        SchoolYearId = schoolYearId;
        GradeId = gradeId;
        Name = name;
        SetCreated();
    }

    /// <summary>Renames the study plan (keeps invariants).</summary>
    public StudyPlan Rename(string newName, string? modifiedBy = null)
    {
        newName = NormalizeName(newName);
        if (string.IsNullOrWhiteSpace(newName)) throw new ArgumentException("Name is required.", nameof(newName));
        if (newName.Length > MaxNameLength) throw new ArgumentException($"Name must be <= {MaxNameLength} chars.", nameof(newName));
        Name = newName;
        SetModified(modifiedBy);
        return this;
    }

    private static string NormalizeName(string value)
    {
        var t = value.Trim();
        while (t.Contains("  ")) t = t.Replace("  ", " ");
        return t;
    }

    public override string ToString() => $"{Name} (Grade:{GradeId})";
}
