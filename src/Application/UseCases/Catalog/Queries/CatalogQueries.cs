using SmartTimetableGenerator.Application.Common.Interfaces;

namespace SmartTimetableGenerator.Application.UseCases.Catalog.Queries;

// Consultas de apoyo para obtener los ids que usan los endpoints de horarios.

public sealed record GetAcademicYearsQuery : IRequest<IReadOnlyList<AcademicYearDto>>;

public sealed record AcademicYearDto(Guid Id, int Year, string Name, DateOnly StartDate, DateOnly EndDate, string Status, IReadOnlyList<AcademicPeriodDto> Periods);

public sealed record AcademicPeriodDto(Guid Id, int Number, string Name, DateOnly StartDate, DateOnly EndDate);

internal sealed class GetAcademicYearsQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetAcademicYearsQuery, IReadOnlyList<AcademicYearDto>>
{
    public async Task<IReadOnlyList<AcademicYearDto>> Handle(GetAcademicYearsQuery request, CancellationToken cancellationToken)
    {
        var years = await dbContext.AcademicYears.AsNoTracking().Include(y => y.Periods).ToListAsync(cancellationToken);

        return years
            .OrderByDescending(y => y.Year)
            .Select(y => new AcademicYearDto(y.Id.Value, y.Year, y.Name, y.StartDate, y.EndDate, y.Status.ToString(),
                y.Periods.Select(p => new AcademicPeriodDto(p.Id.Value, p.Number, p.Name, p.StartDate, p.EndDate)).ToList()))
            .ToList();
    }
}

public sealed record GetCampusesQuery : IRequest<IReadOnlyList<CampusDto>>;

public sealed record CampusDto(Guid Id, string Name, IReadOnlyList<ShiftDto> Shifts);

public sealed record ShiftDto(Guid Id, string Name, string Days, int ClassPeriods);

internal sealed class GetCampusesQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetCampusesQuery, IReadOnlyList<CampusDto>>
{
    public async Task<IReadOnlyList<CampusDto>> Handle(GetCampusesQuery request, CancellationToken cancellationToken)
    {
        var campuses = await dbContext.Campuses.AsNoTracking()
            .Include(c => c.Shifts).ThenInclude(s => s.BellSchedules)
            .ToListAsync(cancellationToken);

        return campuses
            .OrderBy(c => c.Name)
            .Select(c => new CampusDto(c.Id.Value, c.Name,
                c.Shifts.Select(s => new ShiftDto(s.Id.Value, s.Name, s.Days.ToString(),
                    s.BellSchedules.Select(b => b.ClassPeriodCount).DefaultIfEmpty(0).Max())).ToList()))
            .ToList();
    }
}

public sealed record GetCoursesQuery(Guid? AcademicYearId, Guid? CampusId) : IRequest<IReadOnlyList<CourseDto>>;

public sealed record CourseDto(Guid Id, string Name, string Grade, int GradeOrder, Guid CampusId, Guid ShiftId, Guid? HomeRoomId);

internal sealed class GetCoursesQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetCoursesQuery, IReadOnlyList<CourseDto>>
{
    public async Task<IReadOnlyList<CourseDto>> Handle(GetCoursesQuery request, CancellationToken cancellationToken)
    {
        var grades = await dbContext.Grades.AsNoTracking().ToDictionaryAsync(g => g.Id, cancellationToken);
        var courses = await dbContext.Courses.AsNoTracking().ToListAsync(cancellationToken);

        return courses
            .Where(c => request.AcademicYearId is null || c.AcademicYearId.Value == request.AcademicYearId)
            .Where(c => request.CampusId is null || c.CampusId.Value == request.CampusId)
            .Select(c => (Course: c, Grade: grades[c.GradeId]))
            .OrderBy(x => x.Grade.Order).ThenBy(x => x.Course.Name)
            .Select(x => new CourseDto(x.Course.Id.Value, x.Grade.ShortName + x.Course.Name, x.Grade.Name, x.Grade.Order,
                x.Course.CampusId.Value, x.Course.ShiftId.Value, x.Course.HomeRoomId?.Value))
            .ToList();
    }
}

public sealed record GetTeachersQuery : IRequest<IReadOnlyList<TeacherDto>>;

public sealed record TeacherDto(
    Guid Id,
    string FullName,
    string? Email,
    int MaxWeeklyHours,
    int? MaxDailyHours,
    IReadOnlyList<string> Areas,
    bool IsActive,
    string FirstName,
    string LastName,
    string? Phone,
    int? MaxGapsPerDay,
    IReadOnlyList<Guid> AreaIds,
    IReadOnlyList<Guid> CampusIds);

internal sealed class GetTeachersQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetTeachersQuery, IReadOnlyList<TeacherDto>>
{
    public async Task<IReadOnlyList<TeacherDto>> Handle(GetTeachersQuery request, CancellationToken cancellationToken)
    {
        var areas = await dbContext.Areas.AsNoTracking().ToDictionaryAsync(a => a.Id, a => a.Name, cancellationToken);
        var teachers = await dbContext.Teachers.AsNoTracking().ToListAsync(cancellationToken);

        return teachers
            .OrderBy(t => t.LastName).ThenBy(t => t.FirstName)
            .Select(t => new TeacherDto(t.Id.Value, t.FullName, t.Email, t.MaxWeeklyHours, t.MaxDailyHours,
                t.Areas.Select(a => areas.GetValueOrDefault(a.AreaId, "?")).ToList(), t.IsActive,
                t.FirstName, t.LastName, t.Phone, t.MaxGapsPerDay,
                t.Areas.Select(a => a.AreaId.Value).ToList(), t.Campuses.Select(c => c.CampusId.Value).ToList()))
            .ToList();
    }
}
