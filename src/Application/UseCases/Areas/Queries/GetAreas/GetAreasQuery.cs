using SmartTimetableGenerator.Application.Common.Interfaces;

namespace SmartTimetableGenerator.Application.UseCases.Areas.Queries.GetAreas;

/// <summary>
/// Áreas de conocimiento con sus asignaturas, en orden de presentación.
/// </summary>
public sealed record GetAreasQuery(bool IncludeInactiveSubjects = true) : IRequest<IReadOnlyList<AreaDto>>;

public sealed record AreaDto(Guid Id, string Name, int Order, IReadOnlyList<SubjectDto> Subjects);

public sealed record SubjectDto(Guid Id, string Name, string? Code, int Order, bool IsActive);

internal sealed class GetAreasQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetAreasQuery, IReadOnlyList<AreaDto>>
{
    public async Task<IReadOnlyList<AreaDto>> Handle(GetAreasQuery request, CancellationToken cancellationToken)
    {
        var areas = await dbContext.Areas.AsNoTracking().Include(a => a.Subjects).ToListAsync(cancellationToken);

        return areas
            .OrderBy(a => a.Order).ThenBy(a => a.Name)
            .Select(a => new AreaDto(a.Id.Value, a.Name, a.Order,
                a.Subjects
                    .Where(s => request.IncludeInactiveSubjects || s.IsActive)
                    .OrderBy(s => s.Order).ThenBy(s => s.Name)
                    .Select(s => new SubjectDto(s.Id.Value, s.Name, s.Code, s.Order, s.IsActive))
                    .ToList()))
            .ToList();
    }
}
