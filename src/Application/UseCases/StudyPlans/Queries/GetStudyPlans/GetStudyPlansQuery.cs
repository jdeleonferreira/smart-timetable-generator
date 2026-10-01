using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Domain.StudyPlans;

namespace SmartTimetableGenerator.Application.UseCases.StudyPlans.Queries.GetStudyPlans;

/// <summary>
/// Planes de estudio, opcionalmente por año lectivo y sede. El más reciente primero.
/// </summary>
public sealed record GetStudyPlansQuery(Guid? AcademicYearId = null, Guid? CampusId = null) : IRequest<IReadOnlyList<StudyPlanSummaryDto>>;

public sealed record StudyPlanSummaryDto(
    Guid Id,
    Guid AcademicYearId,
    int Year,
    Guid CampusId,
    string CampusName,
    string Name,
    StudyPlanStatus Status,
    DateTimeOffset? ApprovedAt,
    int SubjectCount);

internal sealed class GetStudyPlansQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetStudyPlansQuery, IReadOnlyList<StudyPlanSummaryDto>>
{
    public async Task<IReadOnlyList<StudyPlanSummaryDto>> Handle(GetStudyPlansQuery request, CancellationToken cancellationToken)
    {
        var years = await dbContext.AcademicYears.AsNoTracking().ToDictionaryAsync(y => y.Id, y => y.Year, cancellationToken);
        var campuses = await dbContext.Campuses.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);
        var plans = await dbContext.StudyPlans.AsNoTracking().Include(p => p.Items).ToListAsync(cancellationToken);

        return plans
            .Where(p => request.AcademicYearId is null || p.AcademicYearId.Value == request.AcademicYearId)
            .Where(p => request.CampusId is null || p.CampusId.Value == request.CampusId)
            .Select(p => new StudyPlanSummaryDto(
                p.Id.Value,
                p.AcademicYearId.Value,
                years.GetValueOrDefault(p.AcademicYearId),
                p.CampusId.Value,
                campuses.GetValueOrDefault(p.CampusId, "?"),
                p.Name,
                p.Status,
                p.ApprovedAt,
                p.Items.Count))
            .OrderByDescending(p => p.Year).ThenBy(p => p.CampusName)
            .ToList();
    }
}
