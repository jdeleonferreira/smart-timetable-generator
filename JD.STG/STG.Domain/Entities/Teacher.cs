using STG.Domain.Entities.Base;

namespace STG.Domain.Entities;

/// <summary>
/// Teacher available for assignments within a SchoolYear.
/// Domain rules:
/// 1) Must belong to a SchoolYear (immutable).
/// 2) FullName is required, trimmed, <= 128 chars.
/// 3) MaxDailyPeriods, when set, is 1..20 and overrides global config.
/// </summary>
public sealed class Teacher : Entity
{
    public const int MaxNameLength = 128;
    public const int MaxTagsLength = 256;

    public Guid SchoolYearId { get; private set; }

    public string FullName { get; private set; } = string.Empty;
    public int? MaxDailyPeriods { get; private set; } // null = use SchedulingConfig
    public string? Tags { get; private set; } // optional short metadata (e.g., specialties)

    private Teacher() { } // EF

    public Teacher(Guid schoolYearId, string fullName, int? maxDailyPeriods = null, string? tags = null)
    {
        if (schoolYearId == Guid.Empty) throw new ArgumentException("SchoolYearId is required.", nameof(schoolYearId));

        fullName = NormalizeName(fullName);
        if (string.IsNullOrWhiteSpace(fullName)) throw new ArgumentException("FullName is required.", nameof(fullName));
        if (fullName.Length > MaxNameLength) throw new ArgumentException($"FullName must be <= {MaxNameLength} chars.", nameof(fullName));

        ValidateMaxDaily(maxDailyPeriods);
        if (tags is { Length: > MaxTagsLength }) throw new ArgumentException($"Tags must be <= {MaxTagsLength} chars.", nameof(tags));

        Id = Guid.NewGuid();
        SchoolYearId = schoolYearId;
        FullName = fullName;
        MaxDailyPeriods = maxDailyPeriods;
        Tags = string.IsNullOrWhiteSpace(tags) ? null : tags.Trim();
        SetCreated();
    }

    public Teacher Rename(string newFullName, string? modifiedBy = null)
    {
        newFullName = NormalizeName(newFullName);
        if (string.IsNullOrWhiteSpace(newFullName)) throw new ArgumentException("FullName is required.", nameof(newFullName));
        if (newFullName.Length > MaxNameLength) throw new ArgumentException($"FullName must be <= {MaxNameLength} chars.", nameof(newFullName));
        FullName = newFullName;
        SetModified(modifiedBy);
        return this;
    }

    public Teacher SetMaxDailyPeriods(int? value, string? modifiedBy = null)
    {
        ValidateMaxDaily(value);
        MaxDailyPeriods = value;
        SetModified(modifiedBy);
        return this;
    }

    public Teacher SetTags(string? tags, string? modifiedBy = null)
    {
        if (tags is { Length: > MaxTagsLength }) throw new ArgumentException($"Tags must be <= {MaxTagsLength} chars.", nameof(tags));
        Tags = string.IsNullOrWhiteSpace(tags) ? null : tags.Trim();
        SetModified(modifiedBy);
        return this;
    }

    public override string ToString() => FullName;

    private static string NormalizeName(string value)
    {
        var t = value.Trim();
        while (t.Contains("  ")) t = t.Replace("  ", " ");
        return t;
    }

    private static void ValidateMaxDaily(int? value)
    {
        if (value.HasValue && (value.Value < 1 || value.Value > 20))
            throw new ArgumentOutOfRangeException(nameof(value), "MaxDailyPeriods must be between 1 and 20.");
    }
}
