using System.Text.Json.Serialization;
using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Application.Common.Security;
using SmartTimetableGenerator.Domain.TimetableGeneration;
using SmartTimetableGenerator.Domain.Timetables;

namespace SmartTimetableGenerator.Application.UseCases.Timetables.Commands.GenerateTimetable;

/// <summary>
/// Pone en cola la generación de un horario. Devuelve el id de la solicitud para consultar su estado.
/// </summary>
[Authorize(Roles = Roles.Managers)]
public sealed record GenerateTimetableCommand(int TimeLimitSeconds = GenerateTimetableCommand.DefaultTimeLimitSeconds) : IRequest<ErrorOr<Guid>>
{
    /// <summary>
    /// Límite por defecto. CP-SAT termina antes si demuestra que la solución es óptima;
    /// con 24 cursos la solución óptima (sin huecos) aparece en unos 110 s.
    /// </summary>
    public const int DefaultTimeLimitSeconds = 180;

    [JsonIgnore]
    public Guid TimetableId { get; set; }
}

internal sealed class GenerateTimetableCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<GenerateTimetableCommand, ErrorOr<Guid>>
{
    public async Task<ErrorOr<Guid>> Handle(GenerateTimetableCommand request, CancellationToken cancellationToken)
    {
        var timetableId = TimetableId.From(request.TimetableId);

        var timetable = await dbContext.Timetables
            .FirstOrDefaultAsync(t => t.Id == timetableId, cancellationToken);
        if (timetable is null)
            return TimetableErrors.NotFound;

        if (currentUser.EnsureCanManageCampus(timetable.CampusId) is { } forbidden)
            return forbidden;

        if (timetable.Status != TimetableStatus.Draft)
            return TimetableErrors.NotEditable;

        var busy = await dbContext.GenerationJobs.AnyAsync(
            j => j.TimetableId == timetableId &&
                 (j.Status == GenerationJobStatus.Queued || j.Status == GenerationJobStatus.Running),
            cancellationToken);
        if (busy)
            return GenerationJobErrors.AlreadyRunning;

        var job = GenerationJob.Create(timetableId, timeProvider.GetUtcNow(), request.TimeLimitSeconds, currentUser.UserId);
        dbContext.GenerationJobs.Add(job);
        await dbContext.SaveChangesAsync(cancellationToken);

        return job.Id.Value;
    }
}

internal sealed class GenerateTimetableCommandValidator : AbstractValidator<GenerateTimetableCommand>
{
    public GenerateTimetableCommandValidator()
    {
        RuleFor(v => v.TimetableId).NotEmpty();
        RuleFor(v => v.TimeLimitSeconds)
            .InclusiveBetween(GenerationJob.MinTimeLimitSeconds, GenerationJob.MaxTimeLimitSeconds);
    }
}
