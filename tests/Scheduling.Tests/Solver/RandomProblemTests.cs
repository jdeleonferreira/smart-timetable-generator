using Microsoft.Extensions.Logging.Abstractions;
using SmartTimetableGenerator.Application.Common.Scheduling;
using SmartTimetableGenerator.Infrastructure.Scheduling;
using SmartTimetableGenerator.Scheduling.Tests.Common;

namespace SmartTimetableGenerator.Scheduling.Tests.Solver;

/// <summary>
/// Problemas aleatorios (reproducibles por semilla) con docentes y espacios compartidos, franjas bloqueadas,
/// clases fijadas y límites diarios. Toda solución debe cumplir cada regla y cuadrar las horas.
/// </summary>
public class RandomProblemTests
{
    private static readonly CpSatTimetableSolver Solver = new(NullLogger<CpSatTimetableSolver>.Instance);

    public static TheoryData<int> Seeds => [.. Enumerable.Range(1, 12)];

    [Theory]
    [MemberData(nameof(Seeds))]
    public async Task RandomProblem_ShouldProduceAStrictlyValidSolution(int seed)
    {
        var problem = RandomProblem(seed);

        var solution = await Solver.SolveAsync(problem, CancellationToken.None);

        solution.Status.Should().BeOneOf(SchedulingStatus.Optimal, SchedulingStatus.Feasible);
        solution.ShouldBeValid(problem);
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public async Task RandomProblemWithinCapacity_ShouldPlaceEveryHour(int seed)
    {
        // Con holgura amplia (cada recurso usa como mucho la mitad de sus franjas) todo debe caber
        var problem = RandomProblem(seed, fillRatio: 0.5, withBlocks: false, withFixed: false);

        var solution = await Solver.SolveAsync(problem, CancellationToken.None);

        solution.ShouldBeValid(problem);
        solution.UnplacedHoursByItem.Should().BeEmpty();
        solution.Placed.Should().HaveCount(problem.Items.Sum(i => i.Hours));
    }

    private static SchedulingProblem RandomProblem(int seed, double fillRatio = 0.8, bool withBlocks = true, bool withFixed = true)
    {
        var random = new Random(seed);
        var days = 5;
        var periods = random.Next(5, 8);
        var slots = days * periods;
        var courseCount = random.Next(3, 7);
        var teacherCount = random.Next(2, 5);
        var teacherLoad = new int[teacherCount];
        var items = new List<SchedulingItem>();
        var usedFixed = new HashSet<(string, int, int)>();
        var labUsage = 0;

        for (var c = 0; c < courseCount; c++)
        {
            var course = $"c:{c}";
            var budget = (int)(slots * fillRatio);
            var subjects = random.Next(3, 6);

            for (var s = 0; s < subjects && budget > 0; s++)
            {
                var hours = Math.Min(budget, random.Next(1, 6));
                var teacher = Enumerable.Range(0, teacherCount).OrderBy(t => teacherLoad[t]).First();
                if (teacherLoad[teacher] + hours > (int)(slots * fillRatio))
                    continue;

                teacherLoad[teacher] += hours;
                budget -= hours;

                var resources = new List<string> { course, $"t:{teacher}" };
                if (random.NextDouble() < 0.2 && labUsage + hours <= (int)(slots * fillRatio))
                {
                    resources.Add("s:lab");
                    labUsage += hours;
                }

                var maxPerDay = random.Next(1, 3);
                var maxConsecutive = random.Next(1, 3);
                var fixedSlots = new List<SlotRef>();
                if (withFixed && random.NextDouble() < 0.15)
                {
                    var slot = new SlotRef(random.Next(days), random.Next(periods));
                    if (resources.All(r => usedFixed.Add((r, slot.Day, slot.Period))))
                        fixedSlots.Add(slot);
                }

                items.Add(new SchedulingItem(items.Count, course, resources, hours, maxPerDay, maxConsecutive,
                    RequireContiguousPair: maxPerDay == 2 && maxConsecutive == 2, fixedSlots));
            }
        }

        var blocked = new List<BlockedSlot>();
        if (withBlocks)
        {
            for (var t = 0; t < teacherCount; t++)
            {
                for (var k = 0; k < 3; k++)
                {
                    var (day, period) = (random.Next(days), random.Next(periods));
                    if (!usedFixed.Contains(($"t:{t}", day, period)))
                        blocked.Add(new BlockedSlot($"t:{t}", day, period));
                }
            }
        }

        var loads = Enumerable.Range(0, teacherCount).Select(t => new DailyLoadRule($"t:{t}", 0, periods - 1)).ToList();

        return new SchedulingProblem(days, periods, items, blocked, loads, TimeLimitSeconds: 10);
    }
}
