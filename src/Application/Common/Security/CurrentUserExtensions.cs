using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Domain.Campuses;

namespace SmartTimetableGenerator.Application.Common.Security;

public static class CurrentUserExtensions
{
    public static bool IsInRole(this ICurrentUserService user, string role) =>
        user.Roles.Contains(role, StringComparer.OrdinalIgnoreCase);

    public static bool IsAdmin(this ICurrentUserService user) => user.IsInRole(Roles.Admin);

    /// <summary>
    /// Puede modificar el plan de estudios y los horarios de la sede: el administrador, cualquiera;
    /// el coordinador, solo la suya.
    /// </summary>
    public static bool CanManageCampus(this ICurrentUserService user, CampusId campusId) =>
        user.IsAdmin() || (user.IsInRole(Roles.Coordinator) && user.CampusId == campusId.Value);

    /// <summary>
    /// Ve horarios en borrador (los docentes solo ven los publicados).
    /// </summary>
    public static bool CanSeeDrafts(this ICurrentUserService user) =>
        user.IsAdmin() || user.IsInRole(Roles.Coordinator);

    /// <summary>
    /// Devuelve el error de permisos si no puede gestionar la sede; <c>null</c> si puede.
    /// </summary>
    public static Error? EnsureCanManageCampus(this ICurrentUserService user, CampusId campusId) =>
        user.CanManageCampus(campusId) ? null : SecurityErrors.CampusForbidden;
}
