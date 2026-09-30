using SmartTimetableGenerator.Domain.DayTypes;

namespace SmartTimetableGenerator.Domain.Campuses;

[ValueObject<Guid>]
public readonly partial struct CampusId;

/// <summary>
/// Sede de la institución. Cada sede ofrece sus propios grados y tiene una o varias jornadas.
/// </summary>
public class Campus : AggregateRoot<CampusId>
{
    public const int NameMaxLength = 150;
    public const int CodeMaxLength = 20;
    public const int AddressMaxLength = 250;

    private readonly List<Shift> _shifts = [];

    public string Name { get; private set; } = null!;
    public string? Code { get; private set; }
    public string? Address { get; private set; }
    public bool IsActive { get; private set; }

    public IReadOnlyList<Shift> Shifts => _shifts.AsReadOnly();

    private Campus() { } // Needed for EF Core

    public static Campus Create(string name, string? code = null, string? address = null)
    {
        var campus = new Campus { Id = CampusId.From(Guid.CreateVersion7()), IsActive = true };
        campus.Update(name, code, address);
        return campus;
    }

    public void Update(string name, string? code, string? address)
    {
        Name = TextRules.Required(name, NameMaxLength, nameof(name));
        Code = TextRules.Optional(code, CodeMaxLength, nameof(code));
        Address = TextRules.Optional(address, AddressMaxLength, nameof(address));
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public Shift? FindShift(ShiftId shiftId) => _shifts.FirstOrDefault(s => s.Id == shiftId);

    public ErrorOr<Shift> AddShift(string name, SchoolDays days)
    {
        if (_shifts.Any(s => string.Equals(s.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)))
            return CampusErrors.DuplicateShiftName;

        if (days == SchoolDays.None)
            return CampusErrors.ShiftWithoutDays;

        var shift = Shift.Create(name, days);
        _shifts.Add(shift);
        return shift;
    }

    public ErrorOr<Success> UpdateShift(ShiftId shiftId, string name, SchoolDays days)
    {
        var shift = FindShift(shiftId);
        if (shift is null)
            return CampusErrors.ShiftNotFound;

        if (_shifts.Any(s => s.Id != shiftId && string.Equals(s.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)))
            return CampusErrors.DuplicateShiftName;

        if (days == SchoolDays.None)
            return CampusErrors.ShiftWithoutDays;

        shift.Update(name, days);
        return Result.Success;
    }

    public ErrorOr<Success> RemoveShift(ShiftId shiftId)
    {
        var shift = FindShift(shiftId);
        if (shift is null)
            return CampusErrors.ShiftNotFound;

        _shifts.Remove(shift);
        return Result.Success;
    }

    /// <summary>
    /// Define (o reemplaza) el horario de timbre de una jornada para un tipo de jornada (A, B, …).
    /// </summary>
    public ErrorOr<BellSchedule> SetBellSchedule(ShiftId shiftId, DayTypeId dayTypeId, IEnumerable<BellBlock> blocks)
    {
        var shift = FindShift(shiftId);
        if (shift is null)
            return CampusErrors.ShiftNotFound;

        return shift.SetBellSchedule(dayTypeId, blocks);
    }
}
