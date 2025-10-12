using STG.Domain.Entities.Base;

namespace STG.Domain.Entities;

/// <summary>
/// Represents a curricular Study Area (e.g., Mathematics, Natural Sciences).
/// Aggregate root for <see cref="Subject"/>.
/// </summary>
/// <remarks>
/// Invariants:
/// - Name: required, unique (enforced at persistence).
/// - Code: optional, unique when present.
/// - OrderNo: display/order hint (0..255).
/// </remarks>
public sealed class StudyArea : Entity
{
    private readonly List<Subject> _subjects = [];
    private StudyArea() { } // EF

    public StudyArea(Guid id, string name, string? code, byte? orderNo, bool isActive = true)
    {
        Id = id == default ? Guid.NewGuid() : id;
        Rename(name);
        Recode(code);
        Reorder(orderNo);
        IsActive = isActive;
    }

    /// <summary>Display name (unique, non-empty).</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Optional short code (unique if present).</summary>
    public string? Code { get; private set; }

    /// <summary>Ordering hint for UI/reports.</summary>
    public byte? OrderNo { get; private set; }

    /// <summary>Soft on/off for selection UIs.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Subjects belonging to this StudyArea.</summary>
    public IReadOnlyCollection<Subject> Subjects => _subjects;

    /// <summary>Changes the StudyArea name enforcing non-empty invariant.</summary>
    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("StudyArea name cannot be empty.", nameof(name));
        Name = name.Trim();
    }

    /// <summary>Changes the StudyArea code; normalizes empty inputs to null.</summary>
    public void Recode(string? code)
        => Code = string.IsNullOrWhiteSpace(code) ? null : code!.Trim();

    /// <summary>Sets the display order (0..255).</summary>
    public void Reorder(byte? orderNo) => OrderNo = orderNo;

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
