using STG.Domain.Entities.Base;

namespace STG.Domain.Entities;

/// <summary>
/// Physical classroom or resource room available for scheduling.
/// Domain rules:
/// 1) Must belong to a school year (immutable).
/// 2) Name is required, trimmed, <= 64 chars, unique per school year.
/// 3) Capacity is >= 0 (0 means unspecified).
/// 4) Optional feature flags (e.g., IsLab) and tags are metadata only.
/// </summary>
public sealed class Room : Entity
{
    public const int MaxNameLength = 64;
    public const int MaxTagsLength = 256;

    public Guid SchoolYearId { get; private set; }
    public string Name { get; private set; } = string.Empty;

    public int Capacity { get; private set; } // 0 = unknown/not enforced
    public bool IsLab { get; private set; }   // specialized room indicator
    public string? Tags { get; private set; } // comma-separated keywords (kept small)

    private Room() { } // EF

    /// <summary>Factory constructor that enforces invariants.</summary>
    public Room(Guid schoolYearId, string name, int capacity = 0, bool isLab = false, string? tags = null)
    {
        if (schoolYearId == Guid.Empty) throw new ArgumentException("SchoolYearId is required.", nameof(schoolYearId));
        name = NormalizeName(name);
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (name.Length > MaxNameLength) throw new ArgumentException($"Name must be <= {MaxNameLength} chars.", nameof(name));
        if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be >= 0.");
        if (tags is { Length: > MaxTagsLength }) throw new ArgumentException($"Tags must be <= {MaxTagsLength} chars.", nameof(tags));

        Id = Guid.NewGuid();
        SchoolYearId = schoolYearId;
        Name = name;
        Capacity = capacity;
        IsLab = isLab;
        Tags = string.IsNullOrWhiteSpace(tags) ? null : tags.Trim();
        SetCreated();
    }

    /// <summary>Renames the room (keeps invariants).</summary>
    public Room Rename(string newName, string? modifiedBy = null)
    {
        newName = NormalizeName(newName);
        if (string.IsNullOrWhiteSpace(newName)) throw new ArgumentException("Name is required.", nameof(newName));
        if (newName.Length > MaxNameLength) throw new ArgumentException($"Name must be <= {MaxNameLength} chars.", nameof(newName));
        Name = newName;
        SetModified(modifiedBy);
        return this;
    }

    /// <summary>Sets capacity (0 = unknown/not enforced).</summary>
    public Room SetCapacity(int capacity, string? modifiedBy = null)
    {
        if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be >= 0.");
        Capacity = capacity;
        SetModified(modifiedBy);
        return this;
    }

    /// <summary>Marks this as a lab/specialized room or clears the flag.</summary>
    public Room MarkLab(bool isLab, string? modifiedBy = null)
    {
        IsLab = isLab;
        SetModified(modifiedBy);
        return this;
    }

    /// <summary>Sets lightweight tags (comma-separated), limited to 256 chars.</summary>
    public Room SetTags(string? tags, string? modifiedBy = null)
    {
        if (tags is { Length: > MaxTagsLength }) throw new ArgumentException($"Tags must be <= {MaxTagsLength} chars.", nameof(tags));
        Tags = string.IsNullOrWhiteSpace(tags) ? null : tags.Trim();
        SetModified(modifiedBy);
        return this;
    }

    public override string ToString() => $"{Name} (Cap: {Capacity}{(IsLab ? ", Lab" : "")})";

    private static string NormalizeName(string value)
    {
        var t = value.Trim();
        while (t.Contains("  ")) t = t.Replace("  ", " ");
        return t;
    }
}
