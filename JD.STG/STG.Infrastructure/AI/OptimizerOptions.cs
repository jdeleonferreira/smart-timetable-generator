namespace STG.Infrastructure.AI;

/// <summary>
/// Optional optimizer limits loaded from configuration (used by infra/engines).
/// NOTE: These are config knobs, not domain rules (domain rules están en SchedulingConfig).
/// </summary>
public sealed class OptimizerOptions
{
    public int? MaxPeriodsPerDayTeacher { get; set; }
    public int? MaxPeriodsPerDayGroup { get; set; }
    public int? MaxConsecutiveSameSubject { get; set; }
}
