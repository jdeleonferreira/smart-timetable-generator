using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Domain.TimetableGeneration;

namespace SmartTimetableGenerator.Application.UseCases.GenerationJobs.Queries.GetGenerationJob;

public sealed record GetGenerationJobQuery(Guid JobId) : IRequest<ErrorOr<GenerationJobDto>>;

public sealed record GenerationJobDto(
    Guid Id,
    Guid TimetableId,
    string Status,
    bool IsFinished,
    DateTimeOffset RequestedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    int TimeLimitSeconds,
    int? PlacedLessons,
    int? UnplacedLessons,
    string? Message);

internal sealed class GetGenerationJobQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetGenerationJobQuery, ErrorOr<GenerationJobDto>>
{
    public async Task<ErrorOr<GenerationJobDto>> Handle(GetGenerationJobQuery request, CancellationToken cancellationToken)
    {
        var job = await dbContext.GenerationJobs
            .AsNoTracking()
            .WithSpecification(GenerationJobSpec.ById(GenerationJobId.From(request.JobId)))
            .FirstOrDefaultAsync(cancellationToken);

        if (job is null)
            return GenerationJobErrors.NotFound;

        return new GenerationJobDto(
            job.Id.Value, job.TimetableId.Value, job.Status.ToString(), job.IsFinished,
            job.RequestedAt, job.StartedAt, job.FinishedAt, job.TimeLimitSeconds,
            job.PlacedLessons, job.UnplacedLessons, job.Message);
    }
}
