using STG.Application.Abstractions.AI;
using STG.Domain.Entities;

namespace STG.Infrastructure.AI.OrTools;

public sealed class OrToolsSchedulingOptimizer : ISchedulingOptimizer
{
    public Task<Timetable> OptimizeAsync(
        Timetable seed,
        IReadOnlyList<PeriodSlot> periodGrid,
        IReadOnlyList<Assignment> groupAssignments,
        SchedulingConfig? config,
        CancellationToken ct = default)
    {
        // TODO: implementar con OR-Tools. Por ahora, devuelve el seed sin cambios.
        // Recomendado: construir una nueva instancia Timetable con Entries armadas.
        return Task.FromResult(seed);
    }

    [Obsolete("Use the overload that receives periodGrid and groupAssignments.")]
    public Task<Timetable> OptimizeAsync(
        Timetable seed,
        SchedulingConfig config,
        CancellationToken ct = default)
    {
        // Fallback: si alguien llama el viejo método, falla explícito o intenta resolver internamente.
        throw new NotSupportedException(
            "This overload is obsolete. Use OptimizeAsync(seed, periodGrid, groupAssignments, config, ct).");
    }
}
