using SmartTimetableGenerator.Domain.Campuses;
using SmartTimetableGenerator.Domain.DayTypes;

namespace SmartTimetableGenerator.Domain.AcademicYears;

[ValueObject<Guid>]
public readonly partial struct CalendarEntryId;

public enum CalendarEntryKind
{
    /// <summary>Festivo (automático o manual). Sin clase.</summary>
    Holiday = 0,

    /// <summary>Receso estudiantil / vacaciones. Sin clase.</summary>
    Recess = 1,

    /// <summary>Jornada pedagógica / institucional. Sin clase.</summary>
    PedagogicalDay = 2,

    /// <summary>Otro motivo sin clase.</summary>
    NoClasses = 3,

    /// <summary>Hay clase, pero con otro tipo de jornada (ej.: horario B).</summary>
    SpecialSchedule = 4
}

/// <summary>
/// Excepción del calendario en una fecha: día sin clase o día con tipo de jornada especial.
/// Si <see cref="CampusId"/> es null aplica a toda la institución.
/// </summary>
public class CalendarEntry : Entity<CalendarEntryId>
{
    public const int DescriptionMaxLength = 200;

    public DateOnly Date { get; private set; }
    public CalendarEntryKind Kind { get; private set; }
    public string Description { get; private set; } = null!;
    public CampusId? CampusId { get; private set; }

    /// <summary>Solo para <see cref="CalendarEntryKind.SpecialSchedule"/>.</summary>
    public DayTypeId? DayTypeId { get; private set; }

    /// <summary>True si lo generó el sistema (festivos de Colombia).</summary>
    public bool IsAutomatic { get; private set; }

    private CalendarEntry() { } // Needed for EF Core

    internal static CalendarEntry Create(
        DateOnly date,
        CalendarEntryKind kind,
        string description,
        CampusId? campusId,
        DayTypeId? dayTypeId,
        bool isAutomatic)
    {
        if (kind == CalendarEntryKind.SpecialSchedule && dayTypeId is null)
            throw new ArgumentNullException(nameof(dayTypeId), "Un día con jornada especial requiere el tipo de jornada");

        return new CalendarEntry
        {
            Id = CalendarEntryId.From(Guid.CreateVersion7()),
            Date = date,
            Kind = kind,
            Description = TextRules.Required(description, DescriptionMaxLength, nameof(description)),
            CampusId = campusId,
            DayTypeId = kind == CalendarEntryKind.SpecialSchedule ? dayTypeId : null,
            IsAutomatic = isAutomatic
        };
    }
}
