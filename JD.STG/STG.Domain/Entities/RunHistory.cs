using STG.Domain.Entities.Base;

namespace STG.Domain.Entities;

/// <summary>
/// Execution log for a scheduling run.
/// Domain rules:
/// 1) Must belong to a school year.
/// 2) Status is a small known set (Queued/Running/Succeeded/Failed).
/// 3) Duration and score are optional and set by the engine.
/// </summary>
public sealed class RunHistory : Entity
{
    public Guid SchoolYearId { get; private set; }

    public string RequestedBy { get; private set; } = "system";
    public DateTimeOffset RequestedAt { get; private set; } = DateTimeOffset.UtcNow;

    public string Status { get; private set; } = "Queued"; // Queued/Running/Succeeded/Failed
    public long? DurationMs { get; private set; }
    public double? Score { get; private set; }

    public int? Conflicts { get; private set; }
    public string? LogPointer { get; private set; }

    private RunHistory() { } // EF

    /// <summary>Factory constructor that enforces invariants.</summary>
    public RunHistory(Guid schoolYearId, string? requestedBy = null)
    {
        if (schoolYearId == Guid.Empty) throw new ArgumentException("SchoolYearId is required.", nameof(schoolYearId));

        Id = Guid.NewGuid();
        SchoolYearId = schoolYearId;
        RequestedBy = string.IsNullOrWhiteSpace(requestedBy) ? "system" : requestedBy.Trim();
        RequestedAt = DateTimeOffset.UtcNow;
        Status = "Queued";
        SetCreated();
    }

    /// <summary>Moves the run to Running state.</summary>
    public RunHistory MarkRunning(string? modifiedBy = null)
    {
        Status = "Running";
        SetModified(modifiedBy);
        return this;
    }

    /// <summary>Marks the run as Succeeded and sets outcome metrics.</summary>
    public RunHistory MarkSucceeded(long durationMs, double? score, int? conflicts, string? logPointer, string? modifiedBy = null)
    {
        if (durationMs < 0) throw new ArgumentOutOfRangeException(nameof(durationMs));
        Status = "Succeeded";
        DurationMs = durationMs;
        Score = score;
        Conflicts = conflicts;
        LogPointer = string.IsNullOrWhiteSpace(logPointer) ? null : logPointer.Trim();
        SetModified(modifiedBy);
        return this;
    }

    /// <summary>Marks the run as Failed and optionally sets a log pointer.</summary>
    public RunHistory MarkFailed(long durationMs, string? logPointer, string? modifiedBy = null)
    {
        if (durationMs < 0) throw new ArgumentOutOfRangeException(nameof(durationMs));
        Status = "Failed";
        DurationMs = durationMs;
        LogPointer = string.IsNullOrWhiteSpace(logPointer) ? null : logPointer.Trim();
        SetModified(modifiedBy);
        return this;
    }
}
