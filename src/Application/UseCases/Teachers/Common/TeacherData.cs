using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Domain.Areas;
using SmartTimetableGenerator.Domain.Campuses;
using SmartTimetableGenerator.Domain.Teachers;

namespace SmartTimetableGenerator.Application.UseCases.Teachers.Common;

/// <summary>Datos editables de un docente (los comparten crear y modificar).</summary>
public sealed record TeacherData(
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    int MaxWeeklyHours,
    int? MaxDailyHours,
    int? MaxGapsPerDay,
    IReadOnlyList<Guid> AreaIds,
    IReadOnlyList<Guid> CampusIds);

internal static class TeacherReferences
{
    /// <summary>Comprueba áreas, sedes y correo y aplica los datos al docente.</summary>
    public static async Task<ErrorOr<Success>> ApplyAsync(
        IApplicationDbContext dbContext,
        Teacher teacher,
        TeacherData data,
        CancellationToken cancellationToken)
    {
        var areaIds = data.AreaIds.Distinct().Select(AreaId.From).ToList();
        var knownAreas = await dbContext.Areas.Where(a => areaIds.Contains(a.Id)).CountAsync(cancellationToken);
        if (knownAreas != areaIds.Count)
            return AreaErrors.NotFound;

        var campusIds = data.CampusIds.Distinct().Select(CampusId.From).ToList();
        var knownCampuses = await dbContext.Campuses.Where(c => campusIds.Contains(c.Id)).CountAsync(cancellationToken);
        if (knownCampuses != campusIds.Count)
            return CampusErrors.NotFound;

        var email = string.IsNullOrWhiteSpace(data.Email) ? null : data.Email.Trim().ToLowerInvariant();
        if (email is not null &&
            await dbContext.Teachers.AnyAsync(t => t.Email == email && t.Id != teacher.Id, cancellationToken))
            return TeacherErrors.DuplicateEmail;

        teacher.UpdateContact(data.FirstName, data.LastName, email, data.Phone);

        var workload = teacher.SetWorkload(data.MaxWeeklyHours, data.MaxDailyHours, data.MaxGapsPerDay);
        if (workload.IsError)
            return workload.Errors;

        teacher.SetAreas(areaIds);
        teacher.SetCampuses(campusIds);
        return Result.Success;
    }
}
