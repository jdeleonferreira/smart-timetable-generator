using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Application.Common.Security;
using SmartTimetableGenerator.Domain.Campuses;
using SmartTimetableGenerator.Domain.DayTypes;
using SmartTimetableGenerator.Domain.Timetables;

namespace SmartTimetableGenerator.Application.UseCases.Timetables.Queries.GetTimetableLessons;

/// <summary>
/// Clases de un horario, con nombres y horas. Filtros opcionales: por curso, por docente o por día
/// (vista por curso, por docente e institucional).
/// </summary>
public sealed record GetTimetableLessonsQuery(Guid TimetableId, Guid? CourseId, Guid? TeacherId, DayOfWeek? Day)
    : IRequest<ErrorOr<IReadOnlyList<LessonDto>>>;

public sealed record LessonDto(
    Guid Id,
    DayOfWeek Day,
    int PeriodNumber,
    TimeOnly? Start,
    TimeOnly? End,
    Guid ShiftId,
    string ShiftName,
    Guid CourseId,
    string CourseName,
    Guid SubjectId,
    string SubjectName,
    string? SubjectCode,
    Guid? TeacherId,
    string? TeacherName,
    Guid? SpaceId,
    string? SpaceName,
    bool IsLocked);

internal sealed class GetTimetableLessonsQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser)
    : IRequestHandler<GetTimetableLessonsQuery, ErrorOr<IReadOnlyList<LessonDto>>>
{
    public async Task<ErrorOr<IReadOnlyList<LessonDto>>> Handle(GetTimetableLessonsQuery request, CancellationToken cancellationToken)
    {
        var timetable = await dbContext.Timetables
            .AsNoTracking()
            .WithSpecification(TimetableSpec.ById(TimetableId.From(request.TimetableId)))
            .FirstOrDefaultAsync(cancellationToken);
        if (timetable is null || (timetable.Status != TimetableStatus.Published && !currentUser.CanSeeDrafts()))
            return TimetableErrors.NotFound;

        var campus = await dbContext.Campuses.AsNoTracking()
            .WithSpecification(CampusSpec.ById(timetable.CampusId))
            .FirstAsync(cancellationToken);
        var defaultDayType = await dbContext.DayTypes.AsNoTracking()
            .WithSpecification(DayTypeSpec.Default())
            .FirstOrDefaultAsync(cancellationToken);

        var grades = await dbContext.Grades.AsNoTracking().ToDictionaryAsync(g => g.Id, g => g.ShortName, cancellationToken);
        var courses = await dbContext.Courses.AsNoTracking()
            .Where(c => c.CampusId == timetable.CampusId && c.AcademicYearId == timetable.AcademicYearId)
            .ToDictionaryAsync(c => c.Id, c => grades[c.GradeId] + c.Name, cancellationToken);
        var subjects = (await dbContext.Areas.AsNoTracking().Include(a => a.Subjects).ToListAsync(cancellationToken))
            .SelectMany(a => a.Subjects).ToDictionary(s => s.Id);
        var teachers = await dbContext.Teachers.AsNoTracking()
            .ToDictionaryAsync(t => t.Id, t => t.FirstName + " " + t.LastName, cancellationToken);
        var spaces = await dbContext.Spaces.AsNoTracking()
            .Where(s => s.CampusId == timetable.CampusId)
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        var lessons = timetable.Lessons
            .Where(l => request.CourseId is null || l.CourseId.Value == request.CourseId)
            .Where(l => request.TeacherId is null || (l.TeacherId is { } t && t.Value == request.TeacherId))
            .Where(l => request.Day is null || l.Day == request.Day)
            .OrderBy(l => l.Day == DayOfWeek.Sunday ? 7 : (int)l.Day)
            .ThenBy(l => l.PeriodNumber)
            .Select(l =>
            {
                var shift = campus.FindShift(l.ShiftId);
                var bell = shift is null ? null
                    : (defaultDayType is null ? null : shift.FindBellSchedule(defaultDayType.Id)) ?? shift.BellSchedules.FirstOrDefault();
                var block = bell?.ClassPeriod(l.PeriodNumber);
                var subject = subjects.GetValueOrDefault(l.SubjectId);

                return new LessonDto(
                    l.Id.Value, l.Day, l.PeriodNumber, block?.Start, block?.End,
                    l.ShiftId.Value, shift?.Name ?? "",
                    l.CourseId.Value, courses.GetValueOrDefault(l.CourseId, "?"),
                    l.SubjectId.Value, subject?.Name ?? "?", subject?.Code,
                    l.TeacherId?.Value, l.TeacherId is { } tid ? teachers.GetValueOrDefault(tid) : null,
                    l.SpaceId?.Value, l.SpaceId is { } sid ? spaces.GetValueOrDefault(sid) : null,
                    l.IsLocked);
            })
            .ToList();

        return lessons;
    }
}
