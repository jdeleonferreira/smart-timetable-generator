using STG.Domain.Entities.Base;

namespace STG.Domain.Entities;

/// <summary>
/// Soft-constraint weights for a subject (optionally scoped to a grade).
/// Typical usage: bias early hours for high-energy subjects, avoid long consecutive slots for high-effort ones.
/// Domain rules:
/// 1) Energy/Effort/Focus are integers in range 0..100 (inclusive).
/// 2) Either global-for-subject (GradeId = null) or specific-to-grade (GradeId != null).
/// 3) (SubjectId, GradeId) should be unique (enforced in persistence).
/// </summary>
public sealed class SubjectWeightProfile : Entity
{
    public Guid SubjectId { get; private set; }
    public Guid? GradeId { get; private set; }

    public int Energy { get; private set; }  // 0..100
    public int Effort { get; private set; }  // 0..100
    public int Focus { get; private set; }  // 0..100

    public string? Notes { get; private set; } // optional, max length enforced in persistence

    private SubjectWeightProfile() { } // EF

    /// <summary>Factory constructor that enforces invariants.</summary>
    public SubjectWeightProfile(Guid subjectId, Guid? gradeId, int energy = 50, int effort = 50, int focus = 50, string? notes = null)
    {
        if (subjectId == Guid.Empty) throw new ArgumentException("SubjectId is required.", nameof(subjectId));
        Validate01(energy, nameof(energy));
        Validate01(effort, nameof(effort));
        Validate01(focus, nameof(focus));

        Id = Guid.NewGuid();
        SubjectId = subjectId;
        GradeId = gradeId;
        Energy = energy;
        Effort = effort;
        Focus = focus;
        Notes = notes?.Trim();
        SetCreated();
    }

    /// <summary>Updates the three weights (0..100).</summary>
    public SubjectWeightProfile SetWeights(int energy, int effort, int focus, string? modifiedBy = null)
    {
        Validate01(energy, nameof(energy));
        Validate01(effort, nameof(effort));
        Validate01(focus, nameof(focus));
        Energy = energy;
        Effort = effort;
        Focus = focus;
        SetModified(modifiedBy);
        return this;
    }

    /// <summary>Sets or clears notes.</summary>
    public SubjectWeightProfile SetNotes(string? notes, string? modifiedBy = null)
    {
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        SetModified(modifiedBy);
        return this;
    }

    private static void Validate01(int v, string param)
    {
        if (v < 0 || v > 100) throw new ArgumentOutOfRangeException(param, "Value must be between 0 and 100.");
    }
}
