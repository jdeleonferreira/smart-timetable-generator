using SmartTimetableGenerator.Application.Common.Scheduling;

namespace SmartTimetableGenerator.Scheduling.Tests.Common;

/// <summary>
/// Revisa una solución del motor contra TODAS las reglas del problema, sin reutilizar código del motor.
/// Devuelve la lista de violaciones (vacía si la solución es válida).
/// </summary>
public static class SolutionValidator
{
    public static IReadOnlyList<string> Validate(SchedulingProblem problem, SchedulingSolution solution)
    {
        var errors = new List<string>();
        var items = problem.Items.ToDictionary(i => i.Index);
        var blocked = problem.BlockedSlots.Select(b => (b.ResourceKey, b.Day, b.Period)).ToHashSet();

        if (solution.Status is SchedulingStatus.Infeasible or SchedulingStatus.Unknown)
        {
            if (solution.Placed.Count > 0)
                errors.Add($"Estado {solution.Status} pero hay {solution.Placed.Count} horas ubicadas");
            return errors;
        }

        // Rango y duplicados
        foreach (var h in solution.Placed)
        {
            if (!items.ContainsKey(h.ItemIndex)) errors.Add($"Ítem inexistente {h.ItemIndex}");
            if (h.Day < 0 || h.Day >= problem.Days) errors.Add($"Día fuera de rango: {h}");
            if (h.Period < 0 || h.Period >= problem.PeriodsPerDay) errors.Add($"Franja fuera de rango: {h}");
        }

        foreach (var dup in solution.Placed.GroupBy(h => h).Where(g => g.Count() > 1))
            errors.Add($"Hora duplicada: {dup.Key}");

        foreach (var key in solution.UnplacedHoursByItem.Keys.Where(k => !items.ContainsKey(k)))
            errors.Add($"Horas sin ubicar reportadas para un ítem inexistente {key}");

        if (errors.Count > 0)
            return errors;

        var byItem = solution.Placed.GroupBy(h => h.ItemIndex).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var item in problem.Items)
        {
            var placed = byItem.GetValueOrDefault(item.Index) ?? [];
            var unplaced = solution.UnplacedHoursByItem.GetValueOrDefault(item.Index);

            if (unplaced < 0)
                errors.Add($"Ítem {item.Index}: horas sin ubicar negativas ({unplaced})");

            if (placed.Count + unplaced != item.Hours)
                errors.Add($"Ítem {item.Index}: {placed.Count} ubicadas + {unplaced} sin ubicar ≠ {item.Hours} horas");

            foreach (var f in item.FixedSlots)
            {
                if (!placed.Any(p => p.Day == f.Day && p.Period == f.Period))
                    errors.Add($"Ítem {item.Index}: franja fijada ({f.Day},{f.Period}) no respetada");
            }

            foreach (var p in placed)
            {
                var isFixed = item.FixedSlots.Contains(new SlotRef(p.Day, p.Period));
                if (!isFixed && item.ResourceKeys.Any(r => blocked.Contains((r, p.Day, p.Period))))
                    errors.Add($"Ítem {item.Index}: usa la franja bloqueada ({p.Day},{p.Period})");
            }

            foreach (var day in placed.GroupBy(p => p.Day))
            {
                var periods = day.Select(p => p.Period).OrderBy(p => p).ToList();

                if (periods.Count > item.MaxPerDay)
                    errors.Add($"Ítem {item.Index}: {periods.Count} horas el día {day.Key} (máximo {item.MaxPerDay})");

                var run = 1;
                for (var i = 1; i < periods.Count; i++)
                {
                    run = periods[i] == periods[i - 1] + 1 ? run + 1 : 1;
                    if (run > item.MaxConsecutive)
                        errors.Add($"Ítem {item.Index}: {run} horas seguidas el día {day.Key} (máximo {item.MaxConsecutive})");
                }

                if (item.RequireContiguousPair && periods.Count == 2 && periods[1] != periods[0] + 1)
                    errors.Add($"Ítem {item.Index}: bloque doble partido el día {day.Key} ({periods[0]} y {periods[1]})");
            }
        }

        // Un recurso por franja
        var usage = new Dictionary<(string, int, int), int>();
        foreach (var h in solution.Placed)
        {
            foreach (var r in items[h.ItemIndex].ResourceKeys.Distinct())
            {
                var k = (r, h.Day, h.Period);
                usage[k] = usage.GetValueOrDefault(k) + 1;
            }
        }

        foreach (var ((r, d, p), n) in usage.Where(u => u.Value > 1))
            errors.Add($"Recurso {r} con {n} clases en ({d},{p})");

        // Carga diaria por recurso: el máximo es estricto. El mínimo es una preferencia (cede si no caben las horas),
        // por eso se verifica en las pruebas que lo necesitan y no aquí.
        foreach (var rule in problem.DailyLoads)
        {
            for (var d = 0; d < problem.Days; d++)
            {
                if (rule.Day is { } onlyDay && onlyDay != d)
                    continue;

                var count = Enumerable.Range(0, problem.PeriodsPerDay).Sum(p => usage.GetValueOrDefault((rule.ResourceKey, d, p)));
                if (count > rule.MaxPerDay)
                    errors.Add($"Recurso {rule.ResourceKey}: {count} horas el día {d} (máximo {rule.MaxPerDay})");
            }
        }

        return errors;
    }

    /// <summary>Número de huecos (franja libre entre dos ocupadas) de un recurso en todos los días.</summary>
    public static int Gaps(SchedulingProblem problem, SchedulingSolution solution, string resourceKey)
    {
        var items = problem.Items.ToDictionary(i => i.Index);
        return solution.Placed
            .Where(h => items[h.ItemIndex].ResourceKeys.Contains(resourceKey))
            .GroupBy(h => h.Day)
            .Sum(g => g.Max(h => h.Period) - g.Min(h => h.Period) + 1 - g.Select(h => h.Period).Distinct().Count());
    }

    public static void ShouldBeValid(this SchedulingSolution solution, SchedulingProblem problem)
    {
        var errors = Validate(problem, solution);
        errors.Should().BeEmpty("la solución debe cumplir todas las reglas, pero:\n" + string.Join("\n", errors));
    }
}
