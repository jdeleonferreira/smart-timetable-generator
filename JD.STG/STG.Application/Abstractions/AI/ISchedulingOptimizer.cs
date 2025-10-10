using STG.Domain.Entities;

namespace STG.Application.Abstractions.AI;
public interface ISchedulingOptimizer
{
    Task<Timetable> OptimizeAsync(
        Timetable seed,
        SchedulingConfig config,
        CancellationToken ct = default);
}