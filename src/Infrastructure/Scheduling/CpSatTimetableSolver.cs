using System.Diagnostics;
using Google.OrTools.Sat;
using Microsoft.Extensions.Logging;
using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Application.Common.Scheduling;

namespace SmartTimetableGenerator.Infrastructure.Scheduling;

/// <summary>
/// Motor de horarios con Google OR-Tools CP-SAT.
/// Variables: x[ítem, día, franja] ∈ {0,1}. Restricciones duras: un recurso por franja, máximos por día,
/// horas seguidas, bloques dobles, franjas bloqueadas, franjas fijadas y máximo diario por recurso.
/// Blandas (objetivo, en orden de importancia): horas sin ubicar, horas por debajo del mínimo diario
/// y huecos en la jornada de los cursos. El mínimo diario es blando para que un curso con más horas de
/// las que caben no vuelva imposible todo el horario.
/// </summary>
public sealed class CpSatTimetableSolver(ILogger<CpSatTimetableSolver> logger) : ITimetableSolver
{
    private const int UnplacedHourPenalty = 1000;
    private const int DailyMinimumShortfallPenalty = 50;
    private const int CourseGapPenalty = 3;

    public Task<SchedulingSolution> SolveAsync(SchedulingProblem problem, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(problem);
        return Task.Run(() => Solve(problem, cancellationToken), cancellationToken);
    }

    private SchedulingSolution Solve(SchedulingProblem problem, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var model = new CpModel();
        var days = problem.Days;
        var periods = problem.PeriodsPerDay;

        var blocked = problem.BlockedSlots
            .Select(b => (b.ResourceKey, b.Day, b.Period))
            .ToHashSet();

        // x[item][day, period] (null = franja no disponible para el ítem)
        var x = new Dictionary<int, BoolVar?[,]>();
        var missing = new Dictionary<int, IntVar>();
        var byResource = new Dictionary<string, List<(int Item, BoolVar?[,] Vars)>>();

        foreach (var item in problem.Items)
        {
            var vars = new BoolVar?[days, periods];
            var fixedSlots = item.FixedSlots.ToHashSet();

            for (var d = 0; d < days; d++)
            {
                for (var p = 0; p < periods; p++)
                {
                    var isFixed = fixedSlots.Contains(new SlotRef(d, p));
                    if (!isFixed && item.ResourceKeys.Any(r => blocked.Contains((r, d, p))))
                        continue;

                    var v = model.NewBoolVar($"x_{item.Index}_{d}_{p}");
                    if (isFixed)
                        model.Add(v == 1);

                    vars[d, p] = v;
                }
            }

            x[item.Index] = vars;

            foreach (var key in item.ResourceKeys.Distinct())
            {
                if (!byResource.TryGetValue(key, out var list))
                    byResource[key] = list = [];
                list.Add((item.Index, vars));
            }

            // Horas semanales (con holgura para no fallar si no caben todas)
            var all = Collect(vars);
            var miss = model.NewIntVar(0, item.Hours, $"miss_{item.Index}");
            missing[item.Index] = miss;
            model.Add(LinearExpr.Sum(all) + miss == item.Hours);

            for (var d = 0; d < days; d++)
            {
                var day = CollectDay(vars, d);
                if (day.Count == 0)
                    continue;

                model.Add(LinearExpr.Sum(day) <= item.MaxPerDay);

                // Máximo de horas seguidas: en cualquier ventana de (k+1) franjas caben a lo sumo k
                var k = item.MaxConsecutive;
                if (k < periods)
                {
                    for (var p = 0; p + k < periods; p++)
                    {
                        var window = Enumerable.Range(p, k + 1).Select(q => vars[d, q]).OfType<BoolVar>().ToList();
                        if (window.Count > k)
                            model.Add(LinearExpr.Sum(window) <= k);
                    }
                }

                // Bloque doble: si hay 2 horas en el día, deben ser seguidas
                if (item.RequireContiguousPair)
                {
                    for (var p = 0; p < periods; p++)
                    {
                        for (var q = p + 2; q < periods; q++)
                        {
                            if (vars[d, p] is { } a && vars[d, q] is { } b)
                                model.AddBoolOr([a.Not(), b.Not()]);
                        }
                    }
                }
            }
        }

        // Un recurso (curso, docente, espacio) → una clase por franja
        var busy = new Dictionary<(string Key, int Day, int Period), List<BoolVar>>();
        foreach (var (key, items) in byResource)
        {
            for (var d = 0; d < days; d++)
            {
                for (var p = 0; p < periods; p++)
                {
                    var slot = items.Select(i => i.Vars[d, p]).OfType<BoolVar>().ToList();
                    if (slot.Count > 1)
                        model.Add(LinearExpr.Sum(slot) <= 1);
                    busy[(key, d, p)] = slot;
                }
            }
        }

        // Carga diaria por recurso: máximo estricto, mínimo con holgura penalizada
        var shortfalls = new List<IntVar>();
        foreach (var rule in problem.DailyLoads)
        {
            for (var d = 0; d < days; d++)
            {
                if (rule.Day is { } onlyDay && onlyDay != d)
                    continue;

                var day = Enumerable.Range(0, periods).SelectMany(p => busy.GetValueOrDefault((rule.ResourceKey, d, p)) ?? []).ToList();
                if (day.Count == 0)
                    continue;

                if (rule.MinPerDay > 0)
                {
                    var shortfall = model.NewIntVar(0, rule.MinPerDay, $"short_{rule.ResourceKey}_{d}");
                    model.Add(LinearExpr.Sum(day) + shortfall >= rule.MinPerDay);
                    shortfalls.Add(shortfall);
                }
                if (rule.MaxPerDay < periods)
                    model.Add(LinearExpr.Sum(day) <= rule.MaxPerDay);
            }
        }

        // Objetivo: horas sin ubicar + faltantes del mínimo diario + huecos de los cursos (franja libre seguida de ocupada)
        var objective = new List<LinearExpr>();
        objective.AddRange(missing.Values.Select(m => m * UnplacedHourPenalty));
        objective.AddRange(shortfalls.Select(v => v * DailyMinimumShortfallPenalty));

        foreach (var courseKey in problem.Items.Select(i => i.CourseKey).Distinct())
        {
            for (var d = 0; d < days; d++)
            {
                for (var p = 0; p + 1 < periods; p++)
                {
                    var current = busy.GetValueOrDefault((courseKey, d, p)) ?? [];
                    var next = busy.GetValueOrDefault((courseKey, d, p + 1)) ?? [];
                    if (next.Count == 0)
                        continue;

                    var gap = model.NewBoolVar($"gap_{courseKey}_{d}_{p}");
                    model.Add(gap - LinearExpr.Sum(next) + LinearExpr.Sum(current) >= 0);
                    objective.Add(gap * CourseGapPenalty);
                }
            }
        }

        model.Minimize(LinearExpr.Sum(objective));

        var solver = new CpSolver
        {
            StringParameters = FormattableString.Invariant(
                $"max_time_in_seconds:{problem.TimeLimitSeconds} num_workers:{Math.Max(1, Environment.ProcessorCount)}")
        };

        using var registration = cancellationToken.Register(solver.StopSearch);
        var status = solver.Solve(model);
        stopwatch.Stop();

        logger.LogInformation("CP-SAT terminó con estado {Status} en {Seconds:0.0}s (objetivo {Objective})",
            status, stopwatch.Elapsed.TotalSeconds, status is CpSolverStatus.Optimal or CpSolverStatus.Feasible ? solver.ObjectiveValue : double.NaN);

        var mapped = status switch
        {
            CpSolverStatus.Optimal => SchedulingStatus.Optimal,
            CpSolverStatus.Feasible => SchedulingStatus.Feasible,
            CpSolverStatus.Infeasible => SchedulingStatus.Infeasible,
            _ => SchedulingStatus.Unknown
        };

        if (mapped is SchedulingStatus.Infeasible or SchedulingStatus.Unknown)
            return new SchedulingSolution(mapped, [], new Dictionary<int, int>(), stopwatch.Elapsed.TotalSeconds);

        var placed = new List<PlacedHour>();
        foreach (var (index, vars) in x)
        {
            for (var d = 0; d < days; d++)
            {
                for (var p = 0; p < periods; p++)
                {
                    if (vars[d, p] is { } v && solver.BooleanValue(v))
                        placed.Add(new PlacedHour(index, d, p));
                }
            }
        }

        var unplaced = missing
            .Select(m => (m.Key, Hours: (int)solver.Value(m.Value)))
            .Where(m => m.Hours > 0)
            .ToDictionary(m => m.Key, m => m.Hours);

        return new SchedulingSolution(mapped, placed, unplaced, stopwatch.Elapsed.TotalSeconds);
    }

    private static List<BoolVar> Collect(BoolVar?[,] vars) => vars.Cast<BoolVar?>().OfType<BoolVar>().ToList();

    private static List<BoolVar> CollectDay(BoolVar?[,] vars, int day) =>
        Enumerable.Range(0, vars.GetLength(1)).Select(p => vars[day, p]).OfType<BoolVar>().ToList();
}
