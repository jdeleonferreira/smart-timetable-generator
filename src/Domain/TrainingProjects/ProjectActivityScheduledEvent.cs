namespace SmartTimetableGenerator.Domain.TrainingProjects;

/// <summary>
/// Se programó o reprogramó una actividad. Si trae tipo de jornada (ej.: horario B), la aplicación
/// marca ese día en el calendario del año lectivo.
/// </summary>
public sealed record ProjectActivityScheduledEvent(TrainingProject Project, ProjectActivity Activity) : IDomainEvent;
