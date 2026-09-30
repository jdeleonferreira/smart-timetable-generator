using SmartTimetableGenerator.Domain.Campuses;

namespace SmartTimetableGenerator.Domain.Spaces;

[ValueObject<Guid>]
public readonly partial struct SpaceId;

public enum SpaceType
{
    Classroom = 0,
    Library = 1,
    ComputerLab = 2,
    Laboratory = 3,
    SportsField = 4,
    Auditorium = 5,
    Other = 6
}

/// <summary>
/// Espacio físico de una sede (salón, biblioteca, sala de informática…).
/// Un espacio solo puede tener una clase o actividad a la vez.
/// </summary>
public class Space : AggregateRoot<SpaceId>
{
    public const int NameMaxLength = 100;

    public CampusId CampusId { get; private set; }
    public string Name { get; private set; } = null!;
    public SpaceType Type { get; private set; }

    /// <summary>Capacidad de estudiantes (informativa).</summary>
    public int? Capacity { get; private set; }

    public bool IsActive { get; private set; }

    private Space() { } // Needed for EF Core

    public static Space Create(CampusId campusId, string name, SpaceType type, int? capacity = null)
    {
        var space = new Space { Id = SpaceId.From(Guid.CreateVersion7()), CampusId = campusId, IsActive = true };
        space.Update(name, type, capacity);
        return space;
    }

    public void Update(string name, SpaceType type, int? capacity)
    {
        if (capacity is not null)
            ThrowIfNegativeOrZero(capacity.Value, nameof(capacity));

        Name = TextRules.Required(name, NameMaxLength, nameof(name));
        Type = type;
        Capacity = capacity;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
