// FILE: STG.Application/Services/SchedulingService.cs
using STG.Application.Abstractions.AI;
using STG.Application.Abstractions.Persistence;
using STG.Application.Abstractions.Persistence.Repositories;
using STG.Domain.Entities;

namespace STG.Application.Services;

/// <summary>
/// Orchestrates timetable creation per group using the optimization engine.
/// </summary>
public sealed class SchedulingService
{
    private readonly ISchedulingOptimizer _optimizer;
    private readonly ISchoolYearRepository _years;
    private readonly IGradeRepository _grades;
    private readonly IGroupRepository _groups;
    private readonly IAssignmentRepository _assignments;
    private readonly ITimetableRepository _timetables;
    private readonly ITimetableEntryRepository _entries;
    private readonly IPeriodSlotRepository _slots;
    private readonly ISchedulingConfigRepository _config;
    private readonly IRunHistoryRepository _runs;
    private readonly IUnitOfWork _uow;

    public SchedulingService(
        ISchedulingOptimizer optimizer,
        ISchoolYearRepository years,
        IGradeRepository grades,
        IGroupRepository groups,
        IAssignmentRepository assignments,
        ITimetableRepository timetables,
        ITimetableEntryRepository entries,
        IPeriodSlotRepository slots,
        ISchedulingConfigRepository config,
        IRunHistoryRepository runs,
        IUnitOfWork uow)
    {
        _optimizer = optimizer;
        _years = years;
        _grades = grades;
        _groups = groups;
        _assignments = assignments;
        _timetables = timetables;
        _entries = entries;
        _slots = slots;
        _config = config;
        _runs = runs;
        _uow = uow;
    }

    /// <summary>Runs the optimizer for a given SchoolYear (per group).</summary>
    public async Task<Guid> RunAsync(int year, CancellationToken ct = default)
    {
        var sy = await _years.GetByYearAsync(year, ct)
                 ?? throw new InvalidOperationException($"SchoolYear {year} not found.");

        var periodGrid = await _slots.GetBySchoolYearAsync(sy.Id, ct);
        if (periodGrid.Count == 0) throw new InvalidOperationException("No PeriodSlots configured.");

        var cfg = await _config.GetBySchoolYearAsync(sy.Id, ct);

        var allAssignments = await _assignments.GetBySchoolYearAsync(sy.Id, ct);
        if (allAssignments.Count == 0) throw new InvalidOperationException("No Assignments for this SchoolYear.");

        // Preparar corrida
        var run = new RunHistory(sy.Id, "system");
        await _runs.AddAsync(run, ct);
        await _uow.SaveChangesAsync(ct);

        run = run.MarkRunning();
        await _runs.UpdateAsync(run, ct);
        await _uow.SaveChangesAsync(ct);

        var start = DateTimeOffset.UtcNow;

        // Iterar por grados y grupos del año
        var grades = await _grades.GetBySchoolYearAsync(sy.Id, ct);
        foreach (var grade in grades)
        {
            var groups = await _groups.GetByGradeAsync(grade.Id, ct);
            foreach (var g in groups)
            {
                // assignments del grupo
                var groupAssignments = allAssignments.Where(a => a.GroupId == g.Id).ToList();
                if (groupAssignments.Count == 0) continue; // nada que programar

                // buscar o crear timetable seed
                var timetable = await _timetables.GetForGroupAsync(sy.Id, g.Id, ct)
                               ?? new Timetable(sy.Id, g.Id);

                // llamar optimizador (NUEVA FIRMA)
                var optimized = await _optimizer.OptimizeAsync(
                    timetable,
                    periodGrid,
                    groupAssignments,
                    cfg,
                    ct);

                // persistir timetable + entries devueltos por el engine
                if (timetable.Id == default) // si el repo crea el Id al agregar, ajusta esta condición
                    await _timetables.AddAsync(optimized, ct);

                // upsert entries: aquí asumo que el optimizador devuelve Entries en 'optimized'
                // En una implementación real conviene borrar y reinsertar, o comparar y upsert.
                await _entries.AddRangeAsync(optimized.Entries, ct);
                await _uow.SaveChangesAsync(ct);
            }
        }

        var duration = (long)(DateTimeOffset.UtcNow - start).TotalMilliseconds;
        run = run.MarkSucceeded(durationMs: duration, score: null, conflicts: null, logPointer: null);
        await _runs.UpdateAsync(run, ct);
        await _uow.SaveChangesAsync(ct);

        return run.Id;
    }
}
