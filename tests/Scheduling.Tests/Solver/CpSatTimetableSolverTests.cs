using Microsoft.Extensions.Logging.Abstractions;
using SmartTimetableGenerator.Application.Common.Scheduling;
using SmartTimetableGenerator.Infrastructure.Scheduling;
using SmartTimetableGenerator.Scheduling.Tests.Common;

namespace SmartTimetableGenerator.Scheduling.Tests.Solver;

/// <summary>
/// Escenarios pequeños con resultado conocido. Cada solución se revisa además con <see cref="SolutionValidator"/>.
/// </summary>
public class CpSatTimetableSolverTests
{
    private const int TimeLimit = 20;

    private static readonly CpSatTimetableSolver Solver = new(NullLogger<CpSatTimetableSolver>.Instance);

    private static SchedulingItem Item(
        int index,
        string course,
        int hours,
        string[]? resources = null,
        int? maxPerDay = null,
        int? maxConsecutive = null,
        bool pair = false,
        SlotRef[]? fixedSlots = null) =>
        new(index, course, [course, .. resources ?? []], hours, maxPerDay ?? hours, maxConsecutive ?? 99, pair, fixedSlots ?? []);

    private static SchedulingProblem Problem(
        int days,
        int periods,
        SchedulingItem[] items,
        BlockedSlot[]? blocked = null,
        DailyLoadRule[]? loads = null) =>
        new(days, periods, items, blocked ?? [], loads ?? [], TimeLimit);

    private static async Task<SchedulingSolution> Solve(SchedulingProblem problem)
    {
        var solution = await Solver.SolveAsync(problem, CancellationToken.None);
        solution.ShouldBeValid(problem);
        return solution;
    }

    private static List<int> PeriodsOf(SchedulingSolution s, int item, int day) =>
        s.Placed.Where(h => h.ItemIndex == item && h.Day == day).Select(h => h.Period).OrderBy(p => p).ToList();

    [Fact]
    public async Task EmptyProblem_ShouldBeOptimalWithNothingPlaced()
    {
        var solution = await Solve(Problem(5, 6, []));

        solution.Status.Should().Be(SchedulingStatus.Optimal);
        solution.Placed.Should().BeEmpty();
        solution.UnplacedHoursByItem.Should().BeEmpty();
    }

    [Fact]
    public async Task FeasibleProblem_ShouldPlaceEveryHourExactly()
    {
        var problem = Problem(5, 4,
        [
            Item(0, "c:6A", 5, ["t:mat"], maxPerDay: 2),
            Item(1, "c:6A", 4, ["t:len"], maxPerDay: 1),
            Item(2, "c:6A", 3, ["t:ing"], maxPerDay: 1)
        ]);

        var solution = await Solve(problem);

        solution.Status.Should().Be(SchedulingStatus.Optimal);
        solution.UnplacedHoursByItem.Should().BeEmpty();
        solution.Placed.Count(h => h.ItemIndex == 0).Should().Be(5);
        solution.Placed.Count(h => h.ItemIndex == 1).Should().Be(4);
        solution.Placed.Count(h => h.ItemIndex == 2).Should().Be(3);
    }

    [Fact]
    public async Task SharedTeacher_ShouldNeverTeachTwoCoursesAtOnce_AndFillEverySlotWhenDemandEqualsCapacity()
    {
        // 2 cursos × 5 h con el mismo docente = 10 h en exactamente 10 franjas
        var problem = Problem(2, 5,
        [
            Item(0, "c:6A", 5, ["t:mat"], maxPerDay: 5),
            Item(1, "c:6B", 5, ["t:mat"], maxPerDay: 5)
        ]);

        var solution = await Solve(problem);

        solution.UnplacedHoursByItem.Should().BeEmpty();
        solution.Placed.Select(h => (h.Day, h.Period)).Distinct().Should().HaveCount(10);
    }

    [Fact]
    public async Task SharedSpace_ShouldHostOneClassPerSlot()
    {
        var problem = Problem(1, 4,
        [
            Item(0, "c:10A", 2, ["s:lab"], maxPerDay: 2),
            Item(1, "c:10B", 2, ["s:lab"], maxPerDay: 2)
        ]);

        var solution = await Solve(problem);

        solution.UnplacedHoursByItem.Should().BeEmpty();
        solution.Placed.Select(h => h.Period).Distinct().Should().HaveCount(4);
    }

    [Fact]
    public async Task MaxPerDay_One_ShouldSpreadHoursOverDistinctDays()
    {
        var solution = await Solve(Problem(5, 6, [Item(0, "c:6A", 4, maxPerDay: 1)]));

        solution.Placed.Select(h => h.Day).Distinct().Should().HaveCount(4);
    }

    [Fact]
    public async Task MaxConsecutive_One_ShouldNeverPlaceAdjacentHours()
    {
        // 3 horas en 5 franjas sin dos seguidas: la única opción es 0, 2, 4
        var solution = await Solve(Problem(1, 5, [Item(0, "c:6A", 3, maxPerDay: 3, maxConsecutive: 1)]));

        PeriodsOf(solution, 0, 0).Should().Equal(0, 2, 4);
    }

    [Fact]
    public async Task ContiguousPair_ShouldPlaceTwoHoursOfADayTogether()
    {
        var problem = Problem(2, 6,
        [
            Item(0, "c:6A", 4, maxPerDay: 2, maxConsecutive: 2, pair: true),
            Item(1, "c:6A", 4, maxPerDay: 2, maxConsecutive: 2, pair: true)
        ]);

        var solution = await Solve(problem);

        solution.UnplacedHoursByItem.Should().BeEmpty();
        foreach (var item in new[] { 0, 1 })
        {
            for (var d = 0; d < 2; d++)
            {
                var periods = PeriodsOf(solution, item, d);
                periods.Should().HaveCount(2);
                (periods[1] - periods[0]).Should().Be(1);
            }
        }
    }

    [Fact]
    public async Task BlockedSlots_ShouldNeverBeUsedByTheBlockedResource()
    {
        // Lunes bloqueado por completo: 4 horas con máximo 1 por día solo caben de martes a viernes, una cada día
        var blocked = Enumerable.Range(0, 6).Select(p => new BlockedSlot("t:mat", 0, p)).ToArray();
        var problem = Problem(5, 6, [Item(0, "c:6A", 4, ["t:mat"], maxPerDay: 1)], blocked);

        var solution = await Solve(problem);

        solution.UnplacedHoursByItem.Should().BeEmpty();
        solution.Placed.Select(h => h.Day).OrderBy(d => d).Should().Equal(1, 2, 3, 4);
    }

    [Fact]
    public async Task BlockedSlotsForEveryDay_ShouldReportAllHoursAsUnplaced()
    {
        var blocked = (from d in Enumerable.Range(0, 2) from p in Enumerable.Range(0, 3) select new BlockedSlot("t:mat", d, p)).ToArray();

        var solution = await Solve(Problem(2, 3, [Item(0, "c:6A", 2, ["t:mat"])], blocked));

        solution.Placed.Should().BeEmpty();
        solution.UnplacedHoursByItem[0].Should().Be(2);
    }

    [Fact]
    public async Task FixedSlots_ShouldBeKept_AndCountTowardTheHours()
    {
        var problem = Problem(5, 6, [Item(0, "c:6A", 3, maxPerDay: 1, fixedSlots: [new SlotRef(2, 3)])]);

        var solution = await Solve(problem);

        solution.Placed.Should().ContainSingle(h => h.Day == 2 && h.Period == 3);
        solution.Placed.Should().HaveCount(3);
    }

    [Fact]
    public async Task FixedSlot_ShouldWinOverABlockedSlot()
    {
        var problem = Problem(1, 3,
            [Item(0, "c:6A", 1, ["t:mat"], fixedSlots: [new SlotRef(0, 1)])],
            [new BlockedSlot("t:mat", 0, 1)]);

        var solution = await Solve(problem);

        solution.Placed.Should().ContainSingle(h => h.Day == 0 && h.Period == 1);
    }

    [Fact]
    public async Task DailyLoad_ShouldKeepEachDayWithinMinAndMax()
    {
        var problem = Problem(5, 6,
            [Item(0, "c:6A", 5, maxPerDay: 2), Item(1, "c:6A", 5, maxPerDay: 2)],
            loads: [new DailyLoadRule("c:6A", 2, 2)]);

        var solution = await Solve(problem);

        solution.UnplacedHoursByItem.Should().BeEmpty();
        solution.Placed.GroupBy(h => h.Day).Should().HaveCount(5);
        solution.Placed.GroupBy(h => h.Day).Should().OnlyContain(g => g.Count() == 2);
    }

    [Fact]
    public async Task TeacherDailyMax_ShouldBeRespected()
    {
        var problem = Problem(2, 6,
            [Item(0, "c:6A", 4, ["t:mat"], maxPerDay: 4), Item(1, "c:6B", 4, ["t:mat"], maxPerDay: 4)],
            loads: [new DailyLoadRule("t:mat", 0, 4)]);

        var solution = await Solve(problem);

        solution.UnplacedHoursByItem.Should().BeEmpty();
        solution.Placed.GroupBy(h => h.Day).Should().OnlyContain(g => g.Count() == 4);
    }

    [Fact]
    public async Task CourseOverCapacity_ShouldPlaceAllItCanAndReportTheExactRemainder()
    {
        // 12 horas pedidas en 10 franjas
        var problem = Problem(2, 5, [Item(0, "c:6A", 6), Item(1, "c:6A", 6)]);

        var solution = await Solve(problem);

        solution.Status.Should().Be(SchedulingStatus.Optimal);
        solution.Placed.Should().HaveCount(10);
        solution.UnplacedHoursByItem.Values.Sum().Should().Be(2);
    }

    [Fact]
    public async Task TeacherOverCapacity_ShouldReportTheExactRemainder()
    {
        // 3 cursos × 4 h con el mismo docente = 12 h, pero el docente solo tiene 10 franjas
        var problem = Problem(2, 5,
        [
            Item(0, "c:6A", 4, ["t:mat"]),
            Item(1, "c:6B", 4, ["t:mat"]),
            Item(2, "c:7A", 4, ["t:mat"])
        ]);

        var solution = await Solve(problem);

        solution.Placed.Should().HaveCount(10);
        solution.UnplacedHoursByItem.Values.Sum().Should().Be(2);
    }

    [Fact]
    public async Task ContradictoryFixedSlots_ShouldBeInfeasible()
    {
        // Dos asignaturas del mismo curso fijadas en la misma franja
        var problem = Problem(1, 3,
        [
            Item(0, "c:6A", 1, fixedSlots: [new SlotRef(0, 0)]),
            Item(1, "c:6A", 1, fixedSlots: [new SlotRef(0, 0)])
        ]);

        var solution = await Solve(problem);

        solution.Status.Should().Be(SchedulingStatus.Infeasible);
        solution.Placed.Should().BeEmpty();
    }

    [Fact]
    public async Task ImpossibleDailyMinimum_ShouldYield_AndStillPlaceEveryHour()
    {
        // Mínimo 3 horas diarias, pero la única asignatura admite 1 por día: el mínimo cede, las horas no
        var problem = Problem(2, 4, [Item(0, "c:6A", 2, maxPerDay: 1)], loads: [new DailyLoadRule("c:6A", 3, 4)]);

        var solution = await Solve(problem);

        solution.Status.Should().Be(SchedulingStatus.Optimal);
        solution.UnplacedHoursByItem.Should().BeEmpty();
        solution.Placed.Select(h => h.Day).Distinct().Should().HaveCount(2);
    }

    [Fact]
    public async Task DailyMinimum_ShouldBeMetWhenPossible_EvenIfAnotherDistributionHasNoGaps()
    {
        // 6 horas en 3 días con mínimo 2 por día: la única distribución sin faltantes es 2-2-2
        var problem = Problem(3, 4,
            [Item(0, "c:6A", 3, maxPerDay: 3), Item(1, "c:6A", 3, maxPerDay: 3)],
            loads: [new DailyLoadRule("c:6A", 2, 4)]);

        var solution = await Solve(problem);

        solution.Placed.GroupBy(h => h.Day).Should().HaveCount(3);
        solution.Placed.GroupBy(h => h.Day).Should().OnlyContain(g => g.Count() == 2);
    }

    [Fact]
    public async Task FixedSlotsBreakingMaxPerDay_ShouldBeInfeasible()
    {
        var problem = Problem(1, 4, [Item(0, "c:6A", 2, maxPerDay: 1, fixedSlots: [new SlotRef(0, 0), new SlotRef(0, 2)])]);

        var solution = await Solve(problem);

        solution.Status.Should().Be(SchedulingStatus.Infeasible);
        solution.Placed.Should().BeEmpty();
    }

    [Fact]
    public async Task CourseDay_ShouldStartAtFirstPeriodWithoutGaps()
    {
        var solution = await Solve(Problem(1, 6, [Item(0, "c:6A", 2, maxPerDay: 2), Item(1, "c:6A", 1)]));

        solution.Placed.Select(h => h.Period).OrderBy(p => p).Should().Equal(0, 1, 2);
    }

    [Fact]
    public async Task CourseGaps_ShouldBeZeroWhenAGaplessScheduleExists()
    {
        // El docente no puede en la franja 1 de ningún día: el curso debe organizarse sin huecos igual
        var blocked = Enumerable.Range(0, 3).Select(d => new BlockedSlot("t:mat", d, 1)).ToArray();
        var problem = Problem(3, 5,
        [
            Item(0, "c:6A", 3, ["t:mat"], maxPerDay: 1),
            Item(1, "c:6A", 6, ["t:len"], maxPerDay: 2, maxConsecutive: 2)
        ], blocked);

        var solution = await Solve(problem);

        solution.UnplacedHoursByItem.Should().BeEmpty();
        SolutionValidator.Gaps(problem, solution, "c:6A").Should().Be(0);
    }

    [Fact]
    public async Task CancelledBeforeStart_ShouldThrow()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = () => Solver.SolveAsync(Problem(5, 6, [Item(0, "c:6A", 5)]), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
