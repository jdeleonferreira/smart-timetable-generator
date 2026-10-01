using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Application.Common.Security;
using SmartTimetableGenerator.Domain.Campuses;
using SmartTimetableGenerator.Domain.Teachers;

namespace SmartTimetableGenerator.Application.UseCases.Users.Common;

/// <summary>
/// Normaliza y verifica los datos de un usuario según su rol:
/// el coordinador necesita una sede; el docente, su registro de docente; el administrador no usa ninguno.
/// </summary>
internal static class UserReferences
{
    public static async Task<ErrorOr<UserData>> NormalizeAsync(IApplicationDbContext dbContext, UserData data, CancellationToken cancellationToken)
    {
        var role = Roles.All.FirstOrDefault(r => string.Equals(r, data.Role?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (role is null)
            return UserErrors.InvalidRole;

        var campusId = role == Roles.Coordinator ? data.CampusId : null;
        var teacherId = role is Roles.Teacher or Roles.Coordinator ? data.TeacherId : null;

        if (role == Roles.Coordinator && campusId is null)
            return UserErrors.CampusRequired;

        if (role == Roles.Teacher && teacherId is null)
            return UserErrors.TeacherRequired;

        if (campusId is { } campus && !await dbContext.Campuses.AnyAsync(c => c.Id == CampusId.From(campus), cancellationToken))
            return CampusErrors.NotFound;

        if (teacherId is { } teacher && !await dbContext.Teachers.AnyAsync(t => t.Id == TeacherId.From(teacher), cancellationToken))
            return TeacherErrors.NotFound;

        return data with { Email = data.Email.Trim(), FullName = data.FullName.Trim(), Role = role, CampusId = campusId, TeacherId = teacherId };
    }
}

internal static class UserRules
{
    public const int EmailMaxLength = 256;
    public const int FullNameMaxLength = 150;
    public const int PasswordMinLength = 8;
}
