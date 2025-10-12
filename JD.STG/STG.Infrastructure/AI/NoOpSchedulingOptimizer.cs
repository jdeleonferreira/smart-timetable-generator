using STG.Application.Abstractions.AI;
using STG.Domain.Entities;

namespace STG.Infrastructure.AI;
public class NoOpSchedulingOptimizer : ISchedulingOptimizer
{

    public Task<Timetable> OptimizeAsync(
        Timetable seed, 
        IReadOnlyList<Assignment> groupAssignments, 
        SchedulingConfig? config, 
        CancellationToken ct = default)
    {
        return Task.FromResult(seed);
    }

    public Task<Timetable> OptimizeAsync(Timetable seed, SchedulingConfig config, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}