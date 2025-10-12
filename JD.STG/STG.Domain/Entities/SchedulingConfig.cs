using STG.Domain.Entities.Base;

namespace STG.Domain.Entities;

/// <summary>
/// Global scheduling parameters for a specific <see cref="SchoolYear"/>.
/// Domain rules:
/// 1) Max periods/day for teacher/group are positive and reasonable (1..20).
/// 2) Max consecutive same subject is 1..10.
/// 3) JSON-based feature flags are optional and bounded to 2KB.
/// </summary>
public sealed class SchedulingConfig : Entity
{
    public Guid SchoolYearId { get; private set; }
    public int? MaxPeriodsPerDayTeacher { get; private set; }
    public int? MaxPeriodsPerDayGroup { get; private set; }
    public int? MaxConsecutiveSameSubject { get; private set; }
    public string? PrioritiesJson { get; private set; }

    private SchedulingConfig() { } // EF

    /// <summary>Factory constructor that enforces invariants.</summary>
    public SchedulingConfig(Guid schoolYearId, int? maxPerTeacher = 6, int? maxPerGroup = 8, int? maxConsecutive = 2, string? prioritiesJson = null)
    {
        if (schoolYearId == Guid.Empty) throw new ArgumentException("SchoolYearId is required.", nameof(schoolYearId));
        ValidateRange(maxPerTeacher, 1, 20, nameof(maxPerTeacher));
        ValidateRange(maxPerGroup, 1, 20, nameof(maxPerGroup));
        ValidateRange(maxConsecutive, 1, 10, nameof(maxConsecutive));
        ValidateJsonLen(prioritiesJson);

        Id = Guid.NewGuid();
        SchoolYearId = schoolYearId;
        MaxPeriodsPerDayTeacher = maxPerTeacher;
        MaxPeriodsPerDayGroup = maxPerGroup;
        MaxConsecutiveSameSubject = maxConsecutive;
        PrioritiesJson = prioritiesJson;
        SetCreated();
    }

    /// <summary>Updates limit values (null = engine default).</summary>
    public SchedulingConfig UpdateLimits(int? maxPerTeacher, int? maxPerGroup, int? maxConsecutive, string? modifiedBy = null)
    {
        ValidateRange(maxPerTeacher, 1, 20, nameof(maxPerTeacher));
        ValidateRange(maxPerGroup, 1, 20, nameof(maxPerGroup));
        ValidateRange(maxConsecutive, 1, 10, nameof(maxConsecutive));
        MaxPeriodsPerDayTeacher = maxPerTeacher;
        MaxPeriodsPerDayGroup = maxPerGroup;
        MaxConsecutiveSameSubject = maxConsecutive;
        SetModified(modifiedBy);
        return this;
    }

    /// <summary>Sets feature flags or weights as JSON (up to 2KB).</summary>
    public SchedulingConfig SetPriorities(string? json, string? modifiedBy = null)
    {
        ValidateJsonLen(json);
        PrioritiesJson = json;
        SetModified(modifiedBy);
        return this;
    }

    private static void ValidateRange(int? v, int min, int max, string param)
    {
        if (v.HasValue && (v.Value < min || v.Value > max))
            throw new ArgumentOutOfRangeException(param, $"Value must be between {min} and {max}.");
    }

    private static void ValidateJsonLen(string? json)
    {
        if (json is { Length: > 2000 }) throw new ArgumentException("PrioritiesJson max length is 2000 characters.", nameof(json));
    }
}
