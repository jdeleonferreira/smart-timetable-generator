using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Domain.AcademicYears;
using SmartTimetableGenerator.Domain.Campuses;

namespace SmartTimetableGenerator.Application.UseCases.Teachers.Queries.GetTeachingAssignments;

/// <summary>
/// Asignación académica (qué docente dicta cada asignatura en cada curso) de una sede y año lectivo.
/// </summary>
public sealed record GetTeachingAssignmentsQuery(Guid AcademicYearId, Guid CampusId) : IRequest<IReadOnlyList<TeachingAssignmentDto>>;

public sealed record TeachingAssignmentDto(Guid CourseId, Guid SubjectId, Guid? TeacherId, bool IsManual);

internal sealed class GetTeachingAssignmentsQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetTeachingAssignmentsQuery, IReadOnlyList<TeachingAssignmentDto>>
{
    public async Task<IReadOnlyList<TeachingAssignmentDto>> Handle(GetTeachingAssignmentsQuery request, CancellationToken cancellationToken)
    {
        var yearId = AcademicYearId.From(request.AcademicYearId);
        var campusId = CampusId.From(request.CampusId);

        var courseIds = await dbContext.Courses.AsNoTracking()
            .Where(c => c.AcademicYearId == yearId && c.CampusId == campusId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var assignments = await dbContext.TeachingAssignments.AsNoTracking()
            .Where(a => a.AcademicYearId == yearId && a.AcademicPeriodId == null)
            .ToListAsync(cancellationToken);

        return assignments
            .Where(a => courseIds.Contains(a.CourseId))
            .Select(a => new TeachingAssignmentDto(a.CourseId.Value, a.SubjectId.Value, a.TeacherId?.Value, a.IsManual))
            .ToList();
    }
}
