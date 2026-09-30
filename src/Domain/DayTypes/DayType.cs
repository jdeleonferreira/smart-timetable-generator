namespace SmartTimetableGenerator.Domain.DayTypes;

[ValueObject<Guid>]
public readonly partial struct DayTypeId;

/// <summary>
/// Tipo de jornada: define cómo se reparte el día (ej.: "A" = clases de 45 min, "B" = clases de 40 min).
/// Las franjas concretas (horas de inicio/fin y descansos) se definen por sede y jornada en <c>BellSchedule</c>.
/// </summary>
public class DayType : AggregateRoot<DayTypeId>
{
    public const int CodeMaxLength = 10;
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 500;
    public const int MinClassMinutes = 10;
    public const int MaxClassMinutes = 180;

    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;

    /// <summary>Duración de referencia de una hora de clase, en minutos.</summary>
    public int ClassMinutes { get; private set; }

    /// <summary>El tipo por defecto se usa en todos los días que no tengan otro tipo asignado.</summary>
    public bool IsDefault { get; private set; }

    public string? Description { get; private set; }

    private DayType() { } // Needed for EF Core

    public static DayType Create(string code, string name, int classMinutes, bool isDefault = false, string? description = null)
    {
        var dayType = new DayType { Id = DayTypeId.From(Guid.CreateVersion7()), IsDefault = isDefault };
        dayType.Update(code, name, classMinutes, description);
        return dayType;
    }

    public void Update(string code, string name, int classMinutes, string? description)
    {
        ThrowIfLessThan(classMinutes, MinClassMinutes, nameof(classMinutes));
        ThrowIfGreaterThan(classMinutes, MaxClassMinutes, nameof(classMinutes));

        Code = TextRules.Required(code, CodeMaxLength, nameof(code)).ToUpperInvariant();
        Name = TextRules.Required(name, NameMaxLength, nameof(name));
        ClassMinutes = classMinutes;
        Description = TextRules.Optional(description, DescriptionMaxLength, nameof(description));
    }

    /// <summary>
    /// Solo debe existir un tipo por defecto; la capa de aplicación desmarca el anterior.
    /// </summary>
    public void MarkAsDefault() => IsDefault = true;

    public void UnmarkAsDefault() => IsDefault = false;
}
