using SmartTimetableGenerator.Domain.Areas;
using SmartTimetableGenerator.Domain.Campuses;

namespace SmartTimetableGenerator.Domain.Teachers;

[ValueObject<Guid>]
public readonly partial struct TeacherId;

/// <summary>
/// Docente. Se le asignan áreas de conocimiento (qué puede dictar), sedes donde trabaja,
/// límites de carga y disponibilidad.
/// </summary>
public class Teacher : AggregateRoot<TeacherId>
{
    public const int NameMaxLength = 100;
    public const int EmailMaxLength = 256;
    public const int PhoneMaxLength = 30;
    public const int UserIdMaxLength = 450;
    public const int MaxAllowedWeeklyHours = 60;

    private readonly List<TeacherArea> _areas = [];
    private readonly List<TeacherCampus> _campuses = [];
    private readonly List<AvailabilityRule> _availability = [];

    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string FullName => $"{FirstName} {LastName}";
    public string? Email { get; private set; }
    public string? Phone { get; private set; }

    /// <summary>Usuario de ASP.NET Core Identity vinculado (para el rol Docente).</summary>
    public string? UserId { get; private set; }

    /// <summary>Carga máxima de horas de clase por semana.</summary>
    public int MaxWeeklyHours { get; private set; }

    /// <summary>Máximo de horas de clase en un día. Null = sin límite.</summary>
    public int? MaxDailyHours { get; private set; }

    /// <summary>Máximo de horas libres entre clases en un día. Null = sin límite.</summary>
    public int? MaxGapsPerDay { get; private set; }

    public bool IsActive { get; private set; }

    public IReadOnlyList<TeacherArea> Areas => _areas.AsReadOnly();
    public IReadOnlyList<TeacherCampus> Campuses => _campuses.AsReadOnly();
    public IReadOnlyList<AvailabilityRule> Availability => _availability.AsReadOnly();

    private Teacher() { } // Needed for EF Core

    public static Teacher Create(string firstName, string lastName, int maxWeeklyHours, string? email = null, string? phone = null)
    {
        var teacher = new Teacher { Id = TeacherId.From(Guid.CreateVersion7()), IsActive = true };
        teacher.UpdateContact(firstName, lastName, email, phone);
        var load = teacher.SetWorkload(maxWeeklyHours, null, null);
        if (load.IsError)
            throw new ArgumentOutOfRangeException(nameof(maxWeeklyHours), load.FirstError.Description);

        return teacher;
    }

    public void UpdateContact(string firstName, string lastName, string? email, string? phone)
    {
        FirstName = TextRules.Required(firstName, NameMaxLength, nameof(firstName));
        LastName = TextRules.Required(lastName, NameMaxLength, nameof(lastName));
        Email = TextRules.Optional(email, EmailMaxLength, nameof(email))?.ToLowerInvariant();
        Phone = TextRules.Optional(phone, PhoneMaxLength, nameof(phone));
    }

    public ErrorOr<Success> SetWorkload(int maxWeeklyHours, int? maxDailyHours, int? maxGapsPerDay)
    {
        if (maxWeeklyHours is < 1 or > MaxAllowedWeeklyHours)
            return TeacherErrors.InvalidWorkload;

        if (maxDailyHours is < 1 || maxDailyHours > maxWeeklyHours)
            return TeacherErrors.InvalidWorkload;

        if (maxGapsPerDay is < 0)
            return TeacherErrors.InvalidWorkload;

        MaxWeeklyHours = maxWeeklyHours;
        MaxDailyHours = maxDailyHours;
        MaxGapsPerDay = maxGapsPerDay;
        return Result.Success;
    }

    public void LinkUser(string? userId) => UserId = TextRules.Optional(userId, UserIdMaxLength, nameof(userId));

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public bool CanTeach(AreaId areaId) => _areas.Any(a => a.AreaId == areaId);

    public bool WorksAt(CampusId campusId) => _campuses.Any(c => c.CampusId == campusId);

    public void SetAreas(IEnumerable<AreaId> areaIds)
    {
        ThrowIfNull(areaIds);
        _areas.Clear();
        _areas.AddRange(areaIds.Distinct().Select(id => new TeacherArea(id)));
    }

    public void SetCampuses(IEnumerable<CampusId> campusIds)
    {
        ThrowIfNull(campusIds);
        _campuses.Clear();
        _campuses.AddRange(campusIds.Distinct().Select(id => new TeacherCampus(id)));
    }

    /// <summary>
    /// Reemplaza la disponibilidad: franjas en que no puede (restricción dura) o que prefiere evitar / preferir (blandas).
    /// </summary>
    public ErrorOr<Success> SetAvailability(IEnumerable<AvailabilityRule> rules)
    {
        ThrowIfNull(rules);
        var list = rules.ToList();

        var overlaps = list
            .GroupBy(r => r.Day)
            .Any(g => g.OrderBy(r => r.Start).Zip(g.OrderBy(r => r.Start).Skip(1)).Any(p => p.Second.Start < p.First.End));

        if (overlaps)
            return TeacherErrors.AvailabilityOverlap;

        _availability.Clear();
        _availability.AddRange(list);
        return Result.Success;
    }

    /// <summary>
    /// ¿El docente puede dictar clase en esa franja? (solo evalúa restricciones duras)
    /// </summary>
    public bool IsAvailable(DayOfWeek day, TimeOnly start, TimeOnly end) =>
        !_availability.Any(r => r.Kind == AvailabilityKind.Unavailable && r.Day == day && r.Start < end && start < r.End);
}
