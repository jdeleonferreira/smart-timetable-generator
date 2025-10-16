using STG.Domain.Entities;

namespace STG.Application.Abstractions.AI;

/// <summary>
/// Optimization engine boundary. New overload receives period grid and group assignments.
/// </summary>
public interface ISchedulingOptimizer
{
    /// <summary>
    /// New: Optimize a timetable for a single group given the period grid and that group's assignments.
    /// </summary>
    Task<Timetable> OptimizeAsync(
        Timetable seed,
        IReadOnlyList<Assignment> groupAssignments,
        SchedulingConfig? config,
        CancellationToken ct = default);

    /// <summary>
    /// Legacy overload (kept for backward-compat). Implementations should delegate to the new overload,
    /// obtaining grid & assignments internally or failing fast with a clear message.
    /// </summary>
    [System.Obsolete("Use the overload that receives periodGrid and groupAssignments.")]
    Task<Timetable> OptimizeAsync(
        Timetable seed,
        SchedulingConfig config,
        CancellationToken ct = default);
}
