using SmartTimetableGenerator.Domain.Timetables;

namespace SmartTimetableGenerator.Domain.TimetableGeneration;

[ValueObject<Guid>]
public readonly partial struct GenerationJobId;

public enum GenerationJobStatus
{
    Queued = 0,
    Running = 1,

    /// <summary>Todas las clases quedaron ubicadas.</summary>
    Succeeded = 2,

    /// <summary>Se guardó el mejor horario encontrado, pero quedaron clases sin ubicar.</summary>
    PartiallySucceeded = 3,

    /// <summary>No existe un horario que cumpla las restricciones duras.</summary>
    Infeasible = 4,

    Failed = 5,
    Cancelled = 6
}

/// <summary>
/// Solicitud de generación de un horario. Se ejecuta en segundo plano; el cliente consulta su estado.
/// </summary>
public class GenerationJob : AggregateRoot<GenerationJobId>
{
    public const int MessageMaxLength = 4000;
    public const int RequestedByMaxLength = 256;
    public const int MinTimeLimitSeconds = 5;
    public const int MaxTimeLimitSeconds = 1800;

    public TimetableId TimetableId { get; private set; }
    public GenerationJobStatus Status { get; private set; }
    public string? RequestedBy { get; private set; }
    public DateTimeOffset RequestedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? FinishedAt { get; private set; }

    /// <summary>Tiempo máximo que puede buscar el motor (CP-SAT).</summary>
    public int TimeLimitSeconds { get; private set; }

    public int? PlacedLessons { get; private set; }
    public int? UnplacedLessons { get; private set; }

    /// <summary>Resumen o diagnóstico del resultado (qué restricciones impidieron ubicar clases, errores…).</summary>
    public string? Message { get; private set; }

    public bool IsFinished => Status is GenerationJobStatus.Succeeded or GenerationJobStatus.PartiallySucceeded
        or GenerationJobStatus.Infeasible or GenerationJobStatus.Failed or GenerationJobStatus.Cancelled;

    private GenerationJob() { } // Needed for EF Core

    public static GenerationJob Create(TimetableId timetableId, DateTimeOffset now, int timeLimitSeconds = 60, string? requestedBy = null)
    {
        ThrowIfLessThan(timeLimitSeconds, MinTimeLimitSeconds, nameof(timeLimitSeconds));
        ThrowIfGreaterThan(timeLimitSeconds, MaxTimeLimitSeconds, nameof(timeLimitSeconds));

        return new GenerationJob
        {
            Id = GenerationJobId.From(Guid.CreateVersion7()),
            TimetableId = timetableId,
            Status = GenerationJobStatus.Queued,
            RequestedAt = now,
            TimeLimitSeconds = timeLimitSeconds,
            RequestedBy = TextRules.Optional(requestedBy, RequestedByMaxLength, nameof(requestedBy))
        };
    }

    public ErrorOr<Success> Start(DateTimeOffset now)
    {
        if (Status != GenerationJobStatus.Queued)
            return GenerationJobErrors.InvalidTransition;

        Status = GenerationJobStatus.Running;
        StartedAt = now;
        return Result.Success;
    }

    public ErrorOr<Success> Complete(DateTimeOffset now, int placedLessons, int unplacedLessons, string? message = null)
    {
        if (Status != GenerationJobStatus.Running)
            return GenerationJobErrors.InvalidTransition;

        ThrowIfNegative(placedLessons, nameof(placedLessons));
        ThrowIfNegative(unplacedLessons, nameof(unplacedLessons));

        Status = unplacedLessons == 0 ? GenerationJobStatus.Succeeded : GenerationJobStatus.PartiallySucceeded;
        PlacedLessons = placedLessons;
        UnplacedLessons = unplacedLessons;
        return Finish(now, message);
    }

    public ErrorOr<Success> MarkInfeasible(DateTimeOffset now, string message)
    {
        if (Status != GenerationJobStatus.Running)
            return GenerationJobErrors.InvalidTransition;

        Status = GenerationJobStatus.Infeasible;
        return Finish(now, message);
    }

    public ErrorOr<Success> Fail(DateTimeOffset now, string message)
    {
        if (IsFinished)
            return GenerationJobErrors.InvalidTransition;

        Status = GenerationJobStatus.Failed;
        return Finish(now, message);
    }

    public ErrorOr<Success> Cancel(DateTimeOffset now)
    {
        if (IsFinished)
            return GenerationJobErrors.InvalidTransition;

        Status = GenerationJobStatus.Cancelled;
        return Finish(now, "Cancelado por el usuario");
    }

    private ErrorOr<Success> Finish(DateTimeOffset now, string? message)
    {
        FinishedAt = now;
        Message = message is null || message.Length <= MessageMaxLength ? message : message[..MessageMaxLength];
        return Result.Success;
    }
}
