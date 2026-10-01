using SmartTimetableGenerator.Domain.AcademicYears;
using SmartTimetableGenerator.Domain.Campuses;
using SmartTimetableGenerator.Domain.DayTypes;
using SmartTimetableGenerator.Domain.Teachers;

namespace SmartTimetableGenerator.Domain.TrainingProjects;

[ValueObject<Guid>]
public readonly partial struct TrainingProjectId;

public enum ProjectCategory
{
    /// <summary>Proyecto propio de la institución (ej.: Festival de Porras).</summary>
    Institutional = 0,

    /// <summary>Proyecto obligatorio (Art. 14 Ley 115 de 1994; ej.: Educación para la Democracia).</summary>
    Mandatory = 1
}

/// <summary>
/// Proyecto de formación (Art. 36 Decreto 1860 de 1994) con su responsable y actividades en el calendario.
/// Si <see cref="CampusId"/> es null aplica a toda la institución.
/// </summary>
public class TrainingProject : AggregateRoot<TrainingProjectId>
{
    public const int NameMaxLength = 200;
    public const int ResponsibleNameMaxLength = 200;
    public const int DescriptionMaxLength = 4000;

    private readonly List<ProjectActivity> _activities = [];

    public AcademicYearId AcademicYearId { get; private set; }
    public CampusId? CampusId { get; private set; }
    public string Name { get; private set; } = null!;
    public ProjectCategory Category { get; private set; }

    /// <summary>Docente responsable (si está registrado en el sistema).</summary>
    public TeacherId? ResponsibleTeacherId { get; private set; }

    /// <summary>Nombre del responsable cuando no es un docente registrado, o texto libre.</summary>
    public string? ResponsibleName { get; private set; }

    public string? Description { get; private set; }

    public IReadOnlyList<ProjectActivity> Activities => _activities.OrderBy(a => a.Date).ToList().AsReadOnly();

    private TrainingProject() { } // Needed for EF Core

    public static TrainingProject Create(AcademicYearId academicYearId, string name, ProjectCategory category, CampusId? campusId = null)
    {
        var project = new TrainingProject
        {
            Id = TrainingProjectId.From(Guid.CreateVersion7()),
            AcademicYearId = academicYearId,
            CampusId = campusId
        };

        project.Update(name, category, null);
        return project;
    }

    /// <summary>
    /// Copia el proyecto (sin actividades, porque las fechas cambian) a otro año lectivo.
    /// </summary>
    public static TrainingProject CreateCopy(TrainingProject source, AcademicYearId academicYearId)
    {
        ThrowIfNull(source);

        var copy = Create(academicYearId, source.Name, source.Category, source.CampusId);
        copy.Description = source.Description;
        copy.ResponsibleTeacherId = source.ResponsibleTeacherId;
        copy.ResponsibleName = source.ResponsibleName;
        return copy;
    }

    public void Update(string name, ProjectCategory category, string? description)
    {
        Name = TextRules.Required(name, NameMaxLength, nameof(name));
        Category = category;
        Description = TextRules.Optional(description, DescriptionMaxLength, nameof(description));
    }

    public void SetResponsible(TeacherId? teacherId, string? responsibleName)
    {
        ResponsibleTeacherId = teacherId;
        ResponsibleName = TextRules.Optional(responsibleName, ResponsibleNameMaxLength, nameof(responsibleName));
    }

    public ProjectActivity? FindActivity(ProjectActivityId activityId) => _activities.FirstOrDefault(a => a.Id == activityId);

    /// <summary>
    /// Agrega una actividad. Si indica <paramref name="dayTypeId"/> (ej.: horario B), ese día pasa a esa jornada;
    /// la capa de aplicación la registra en el calendario del año lectivo.
    /// </summary>
    public ErrorOr<ProjectActivity> AddActivity(
        string title,
        DateOnly date,
        TimeOnly? startTime = null,
        TimeOnly? endTime = null,
        DayTypeId? dayTypeId = null,
        string? description = null)
    {
        var activity = ProjectActivity.Create(title, date, startTime, endTime, dayTypeId, description);
        if (activity.IsError)
            return activity.Errors;

        _activities.Add(activity.Value);
        AddDomainEvent(new ProjectActivityScheduledEvent(this, activity.Value));
        return activity.Value;
    }

    public ErrorOr<Success> UpdateActivity(
        ProjectActivityId activityId,
        string title,
        DateOnly date,
        TimeOnly? startTime,
        TimeOnly? endTime,
        DayTypeId? dayTypeId,
        string? description)
    {
        var activity = FindActivity(activityId);
        if (activity is null)
            return TrainingProjectErrors.ActivityNotFound;

        var result = activity.Update(title, date, startTime, endTime, dayTypeId, description);
        if (result.IsError)
            return result.Errors;

        AddDomainEvent(new ProjectActivityScheduledEvent(this, activity));
        return Result.Success;
    }

    public ErrorOr<Success> RemoveActivity(ProjectActivityId activityId)
    {
        var activity = FindActivity(activityId);
        if (activity is null)
            return TrainingProjectErrors.ActivityNotFound;

        _activities.Remove(activity);
        return Result.Success;
    }
}
