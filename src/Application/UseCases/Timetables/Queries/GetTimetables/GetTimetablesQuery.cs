using SmartTimetableGenerator.Application.Common.Interfaces;

namespace SmartTimetableGenerator.Application.UseCases.Timetables.Queries.GetTimetables;

public sealed record GetTimetablesQuery(Guid? AcademicYearId, Guid? CampusId) : IRequest<IReadOnlyList<TimetableSummaryDto>>;

public sealed record TimetableSummaryDto(
    Guid Id,
    string Name,
    string Status,
    Guid AcademicYearId,
    Guid CampusId,
    Guid AcademicPeriodId,
    int LessonCount,
    DateTimeOffset? PublishedAt);

internal sealed class GetTimetablesQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetTimetablesQuery, IReadOnlyList<TimetableSummaryDto>>
{
    public async Task<IReadOnlyList<TimetableSummaryDto>> Handle(GetTimetablesQuery request, CancellationToken cancellationToken)
    {
        var timetables = await dbContext.Timetables
            .AsNoTracking()
            .Include(t => t.Lessons)
            .ToListAsync(cancellationToken);

        return timetables
            .Where(t => request.AcademicYearId is null || t.AcademicYearId.Value == request.AcademicYearId)
            .Where(t => request.CampusId is null || t.CampusId.Value == request.CampusId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TimetableSummaryDto(
                t.Id.Value, t.Name, t.Status.ToString(), t.AcademicYearId.Value, t.CampusId.Value,
                t.AcademicPeriodId.Value, t.Lessons.Count, t.PublishedAt))
            .ToList();
    }
}
