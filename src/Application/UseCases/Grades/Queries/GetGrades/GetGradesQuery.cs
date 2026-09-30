using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Domain.Grades;

namespace SmartTimetableGenerator.Application.UseCases.Grades.Queries.GetGrades;

/// <summary>
/// Grados del catálogo institucional, en orden de presentación (Preescolar, 1º … 11º).
/// </summary>
public sealed record GetGradesQuery : IRequest<IReadOnlyList<GradeDto>>;

public sealed record GradeDto(Guid Id, string Name, string ShortName, EducationLevel Level, int Order);

internal sealed class GetGradesQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetGradesQuery, IReadOnlyList<GradeDto>>
{
    public async Task<IReadOnlyList<GradeDto>> Handle(GetGradesQuery request, CancellationToken cancellationToken)
    {
        var grades = await dbContext.Grades.AsNoTracking().ToListAsync(cancellationToken);

        return grades
            .OrderBy(g => g.Order).ThenBy(g => g.Name)
            .Select(g => new GradeDto(g.Id.Value, g.Name, g.ShortName, g.Level, g.Order))
            .ToList();
    }
}
