using SmartTimetableGenerator.Domain.DayTypes;

namespace SmartTimetableGenerator.Domain.TrainingProjects;

[ValueObject<Guid>]
public readonly partial struct ProjectActivityId;

/// <summary>
/// Actividad de un proyecto de formación en una fecha del calendario.
/// </summary>
public class ProjectActivity : Entity<ProjectActivityId>
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 2000;

    public string Title { get; private set; } = null!;
    public DateOnly Date { get; private set; }
    public TimeOnly? StartTime { get; private set; }
    public TimeOnly? EndTime { get; private set; }

    /// <summary>Tipo de jornada que se aplica ese día por la actividad (ej.: horario B). Null = no cambia la jornada.</summary>
    public DayTypeId? DayTypeId { get; private set; }

    public string? Description { get; private set; }

    private ProjectActivity() { } // Needed for EF Core

    internal static ErrorOr<ProjectActivity> Create(
        string title,
        DateOnly date,
        TimeOnly? startTime,
        TimeOnly? endTime,
        DayTypeId? dayTypeId,
        string? description)
    {
        var activity = new ProjectActivity { Id = ProjectActivityId.From(Guid.CreateVersion7()) };
        var result = activity.Update(title, date, startTime, endTime, dayTypeId, description);
        return result.IsError ? result.Errors : activity;
    }

    internal ErrorOr<Success> Update(
        string title,
        DateOnly date,
        TimeOnly? startTime,
        TimeOnly? endTime,
        DayTypeId? dayTypeId,
        string? description)
    {
        if (startTime is not null && endTime is not null && endTime <= startTime)
            return TrainingProjectErrors.InvalidActivityTime;

        Title = TextRules.Required(title, TitleMaxLength, nameof(title));
        Date = date;
        StartTime = startTime;
        EndTime = endTime;
        DayTypeId = dayTypeId;
        Description = TextRules.Optional(description, DescriptionMaxLength, nameof(description));
        return Result.Success;
    }
}
