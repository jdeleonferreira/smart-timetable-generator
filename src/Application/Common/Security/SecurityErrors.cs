namespace SmartTimetableGenerator.Application.Common.Security;

public static class SecurityErrors
{
    public static readonly Error Unauthenticated = Error.Unauthorized(
        "Security.Unauthenticated",
        "Debe iniciar sesión");

    public static readonly Error Forbidden = Error.Forbidden(
        "Security.Forbidden",
        "No tiene permiso para esta acción");

    public static readonly Error CampusForbidden = Error.Forbidden(
        "Security.CampusForbidden",
        "Solo puede gestionar la sede que tiene asignada");
}
