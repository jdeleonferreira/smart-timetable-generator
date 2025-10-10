// FILE: STG.Domain/Entities/Grade.cs
using STG.Domain.Entities.Base;

namespace STG.Domain.Entities;

/// <summary>
/// Aggregate that represents an academic grade within a given <see cref="SchoolYear"/>.
/// Domain rules:
/// 1) A grade belongs to exactly one school year (immutable).
/// 2) Name is required, trimmed, <= 64 chars, unique per school year.
/// 3) Optional numeric <see cref="Order"/> supports deterministic sorting (0 = unset).
/// 4) Owns multiple <see cref="Group"/> child entities.
/// </summary>
public sealed class Grade : Entity
{
    public const int MaxNameLength = 64;

    /// <summary>Owning school year (immutable after creation).</summary>
    public Guid SchoolYearId { get; private set; }

    /// <summary>Human-friendly display name (e.g., "Preschool", "1", "Grade 6").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Optional deterministic sort order. 0 means unset.</summary>
    public byte Order { get; private set; }

    /// <summary>Child groups (A, B, C...), owned by this aggregate.</summary>
    public IReadOnlyCollection<Group> Groups => _groups.AsReadOnly();
    private readonly List<Group> _groups = new();

    private Grade() { } // EF

    /// <summary>Factory constructor that enforces invariants.</summary>
    public Grade(Guid schoolYearId, string name, byte order = 0)
    {
        if (schoolYearId == Guid.Empty) throw new ArgumentException("SchoolYearId is required.", nameof(schoolYearId));
        name = NormalizeName(name);
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (name.Length > MaxNameLength) throw new ArgumentException($"Name must be <= {MaxNameLength} chars.", nameof(name));
        if (order is > 30) throw new ArgumentOutOfRangeException(nameof(order), "Order must be 0 or between 1 and 30.");

        Id = Guid.NewGuid();
        SchoolYearId = schoolYearId;
        Name = name;
        Order = order;
        SetCreated();
    }

    /// <summary>Renames the grade (keeps invariants).</summary>
    public Grade Rename(string newName, string? modifiedBy = null)
    {
        newName = NormalizeName(newName);
        if (string.IsNullOrWhiteSpace(newName)) throw new ArgumentException("Name is required.", nameof(newName));
        if (newName.Length > MaxNameLength) throw new ArgumentException($"Name must be <= {MaxNameLength} chars.", nameof(newName));
        Name = newName;
        SetModified(modifiedBy);
        return this;
    }

    /// <summary>Sets the optional sort order. Use 0 to unset.</summary>
    public Grade SetOrder(byte order, string? modifiedBy = null)
    {
        if (order is > 30) throw new ArgumentOutOfRangeException(nameof(order), "Order must be 0 or between 1 and 30.");
        Order = order;
        SetModified(modifiedBy);
        return this;
    }

    /// <summary>Creates and adds a new group under this grade (e.g., "A").</summary>
    public Group AddGroup(string name, string? createdBy = null)
    {
        var group = new Group(this.Id, name,"",30); //TODO: Check size limit?
        _groups.Add(group);
        SetModified(createdBy);
        return group;
    }

    public override string ToString() => $"{Name} (Order: {Order})";

    private static string NormalizeName(string value)
    {
        var t = value.Trim();
        while (t.Contains("  ")) t = t.Replace("  ", " ");
        return t;
    }
}
