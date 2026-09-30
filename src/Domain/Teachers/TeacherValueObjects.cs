using SmartTimetableGenerator.Domain.Areas;
using SmartTimetableGenerator.Domain.Campuses;

namespace SmartTimetableGenerator.Domain.Teachers;

/// <summary>Área de conocimiento que el docente puede dictar.</summary>
public record TeacherArea : IValueObject
{
    // Private setter needed for EF
    public AreaId AreaId { get; private set; }

    public TeacherArea(AreaId areaId) => AreaId = areaId;
}

/// <summary>Sede donde trabaja el docente.</summary>
public record TeacherCampus : IValueObject
{
    // Private setter needed for EF
    public CampusId CampusId { get; private set; }

    public TeacherCampus(CampusId campusId) => CampusId = campusId;
}

public enum AvailabilityKind
{
    /// <summary>No puede dictar clase (restricción dura).</summary>
    Unavailable = 0,

    /// <summary>Prefiere no dictar clase (restricción blanda).</summary>
    Avoid = 1,

    /// <summary>Prefiere dictar clase en esa franja (restricción blanda).</summary>
    Prefer = 2
}

/// <summary>
/// Regla de disponibilidad por día y rango de horas. Se expresa en horas (no en número de franja)
/// porque el docente puede trabajar en sedes y jornadas con franjas distintas.
/// </summary>
public record AvailabilityRule : IValueObject
{
    // Private setters needed for EF
    public DayOfWeek Day { get; private set; }
    public TimeOnly Start { get; private set; }
    public TimeOnly End { get; private set; }
    public AvailabilityKind Kind { get; private set; }

    public AvailabilityRule(DayOfWeek day, TimeOnly start, TimeOnly end, AvailabilityKind kind)
    {
        if (end <= start)
            throw new ArgumentOutOfRangeException(nameof(end), "La hora final debe ser posterior a la inicial");

        Day = day;
        Start = start;
        End = end;
        Kind = kind;
    }
}
