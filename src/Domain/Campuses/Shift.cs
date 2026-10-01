using SmartTimetableGenerator.Domain.DayTypes;

namespace SmartTimetableGenerator.Domain.Campuses;

[ValueObject<Guid>]
public readonly partial struct ShiftId;

/// <summary>
/// Jornada de una sede (Mañana, Tarde, Sabatina…). Define los días de clase y,
/// por cada tipo de jornada, su horario de timbre (franjas).
/// </summary>
public class Shift : Entity<ShiftId>
{
    public const int NameMaxLength = 50;

    private readonly List<BellSchedule> _bellSchedules = [];

    public string Name { get; private set; } = null!;

    public SchoolDays Days { get; private set; }

    public IReadOnlyList<BellSchedule> BellSchedules => _bellSchedules.AsReadOnly();

    private Shift() { } // Needed for EF Core

    internal static Shift Create(string name, SchoolDays days)
    {
        var shift = new Shift { Id = ShiftId.From(Guid.CreateVersion7()) };
        shift.Update(name, days);
        return shift;
    }

    internal void Update(string name, SchoolDays days)
    {
        Name = TextRules.Required(name, NameMaxLength, nameof(name));
        Days = days;
    }

    public BellSchedule? FindBellSchedule(DayTypeId dayTypeId) =>
        _bellSchedules.FirstOrDefault(b => b.DayTypeId == dayTypeId);

    internal ErrorOr<BellSchedule> SetBellSchedule(DayTypeId dayTypeId, IEnumerable<BellBlock> blocks)
    {
        var existing = FindBellSchedule(dayTypeId);
        if (existing is not null)
        {
            var updated = existing.ReplaceBlocks(blocks);
            return updated.IsError ? updated.Errors : existing;
        }

        var created = BellSchedule.Create(dayTypeId, blocks);
        if (created.IsError)
            return created.Errors;

        _bellSchedules.Add(created.Value);
        return created.Value;
    }
}
