using SmartTimetableGenerator.Domain.DayTypes;

namespace SmartTimetableGenerator.Domain.Campuses;

[ValueObject<Guid>]
public readonly partial struct BellScheduleId;

/// <summary>
/// Horario de timbre de una jornada para un tipo de jornada: secuencia de franjas de clase y descansos.
/// Las clases se numeran 1..N en orden; el horario generado se expresa en (día, número de franja),
/// por eso un mismo horario sirve para el tipo A (45 min) y el B (40 min).
/// </summary>
public class BellSchedule : Entity<BellScheduleId>
{
    private readonly List<BellBlock> _blocks = [];

    public DayTypeId DayTypeId { get; private set; }

    public IReadOnlyList<BellBlock> Blocks => _blocks.AsReadOnly();

    public int ClassPeriodCount => _blocks.Count(b => b.Kind == BellBlockKind.Class);

    private BellSchedule() { } // Needed for EF Core

    internal static ErrorOr<BellSchedule> Create(DayTypeId dayTypeId, IEnumerable<BellBlock> blocks)
    {
        var schedule = new BellSchedule { Id = BellScheduleId.From(Guid.CreateVersion7()), DayTypeId = dayTypeId };
        var result = schedule.ReplaceBlocks(blocks);
        return result.IsError ? result.Errors : schedule;
    }

    internal ErrorOr<Success> ReplaceBlocks(IEnumerable<BellBlock> blocks)
    {
        ThrowIfNull(blocks);

        var ordered = blocks.OrderBy(b => b.Start).ToList();

        if (ordered.Count(b => b.Kind == BellBlockKind.Class) == 0)
            return CampusErrors.BellScheduleWithoutClasses;

        for (var i = 1; i < ordered.Count; i++)
        {
            if (ordered[i].Start < ordered[i - 1].End)
                return CampusErrors.BellBlocksOverlap;
        }

        _blocks.Clear();

        var periodNumber = 0;
        foreach (var block in ordered)
        {
            var number = block.Kind == BellBlockKind.Class ? ++periodNumber : (int?)null;
            _blocks.Add(new BellBlock(block.Kind, block.Start, block.End, block.Label, number));
        }

        return Result.Success;
    }

    /// <summary>
    /// Franja de clase por número (1..N), o null si no existe.
    /// </summary>
    public BellBlock? ClassPeriod(int periodNumber) =>
        _blocks.FirstOrDefault(b => b.Kind == BellBlockKind.Class && b.PeriodNumber == periodNumber);
}
