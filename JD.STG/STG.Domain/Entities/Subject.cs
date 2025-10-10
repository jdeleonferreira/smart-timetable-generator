using STG.Domain.Entities.Base;

namespace STG.Domain.Entities;

/// <summary>
/// Academic subject that can be assigned to groups/teachers within a SchoolYear.
/// Domain rules:
/// 1) Must belong to a SchoolYear (immutable reference).
/// 2) Name is required, trimmed, <= 64 chars; unique per SchoolYear.
/// 3) Optional short Code (<= 16 chars) is uppercased; unique per SchoolYear when provided.
/// </summary>
public sealed class Subject : Entity
{
    public const int MaxNameLength = 64;
    public const int MaxCodeLength = 16;

    public Guid SchoolYearId { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public string? Code { get; private set; } // optional, uppercased
    public string? Area { get; private set; } // optional taxonomy (kept small in persistence)

    private Subject() { } // EF

    /// <summary>Factory constructor that enforces invariants.</summary>
    public Subject(Guid schoolYearId, string name, string? code = null, string? area = null)
    {
        if (schoolYearId == Guid.Empty) throw new ArgumentException("SchoolYearId is required.", nameof(schoolYearId));

        name = NormalizeName(name);
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (name.Length > MaxNameLength) throw new ArgumentException($"Name must be <= {MaxNameLength} chars.", nameof(name));

        code = NormalizeCode(code);
        if (code is { Length: > MaxCodeLength }) throw new ArgumentException($"Code must be <= {MaxCodeLength} chars.", nameof(code));

        Id = Guid.NewGuid();
        SchoolYearId = schoolYearId;
        Name = name;
        Code = code;
        Area = string.IsNullOrWhiteSpace(area) ? null : area.Trim();
        SetCreated();
    }

    /// <summary>Renames the subject.</summary>
    public Subject Rename(string newName, string? modifiedBy = null)
    {
        newName = NormalizeName(newName);
        if (string.IsNullOrWhiteSpace(newName)) throw new ArgumentException("Name is required.", nameof(newName));
        if (newName.Length > MaxNameLength) throw new ArgumentException($"Name must be <= {MaxNameLength} chars.", nameof(newName));
        Name = newName;
        SetModified(modifiedBy);
        return this;
    }

    /// <summary>Sets or clears the short code (uppercased).</summary>
    public Subject SetCode(string? code, string? modifiedBy = null)
    {
        code = NormalizeCode(code);
        if (code is { Length: > MaxCodeLength }) throw new ArgumentException($"Code must be <= {MaxCodeLength} chars.", nameof(code));
        Code = code;
        SetModified(modifiedBy);
        return this;
    }

    /// <summary>Sets or clears the taxonomy area.</summary>
    public Subject SetArea(string? area, string? modifiedBy = null)
    {
        Area = string.IsNullOrWhiteSpace(area) ? null : area.Trim();
        SetModified(modifiedBy);
        return this;
    }

    public override string ToString() => Code is null ? Name : $"{Code} - {Name}";

    private static string NormalizeName(string value)
    {
        var t = value.Trim();
        while (t.Contains("  ")) t = t.Replace("  ", " ");
        return t;
    }

    private static string? NormalizeCode(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
}
