using STG.Domain.Entities.Base;
using STG.Domain.ValueObjects;

namespace STG.Domain.Entities;

/// <summary>
/// Aggregate de horario semanal (resultado del solver).
/// </summary>
public class Timetable : AggregateRoot
{
    private readonly List<Assignment> _assignments = new();
    public IReadOnlyList<Assignment> Assignments => _assignments;

    public int Year { get; private set; }
    public WeekConfig WeekConfig { get; private set; }

    private Timetable() { }

    public Timetable(int year, WeekConfig weekConfig)
    {
        if (year < 2000) throw new ArgumentOutOfRangeException(nameof(year));
        Year = year;
        WeekConfig = weekConfig ?? throw new ArgumentNullException(nameof(weekConfig));
    }

    public void AddAssignment(Assignment a)
    {
        if (a is null) throw new ArgumentNullException(nameof(a));
        if (!WeekConfig.IsValidSlot(a.Slot))
            throw new InvalidOperationException("Assignment slot is not valid for the configured week.");

        // Reglas duras simples: no solapar en mismo slot por Group/Teacher/Room
        if (_assignments.Any(x => x.Slot.Equals(a.Slot) && x.GroupCode == a.GroupCode))
            throw new InvalidOperationException("Group overlap.");
        if (_assignments.Any(x => x.Slot.Equals(a.Slot) && x.Teacher == a.Teacher))
            throw new InvalidOperationException("Teacher overlap.");
        if (_assignments.Any(x => x.Slot.Equals(a.Slot) && x.Room == a.Room))
            throw new InvalidOperationException("Room overlap.");

        _assignments.Add(a);
    }
}


