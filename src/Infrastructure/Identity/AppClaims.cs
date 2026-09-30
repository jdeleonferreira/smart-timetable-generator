namespace SmartTimetableGenerator.Infrastructure.Identity;

/// <summary>
/// Nombres de los claims del token.
/// </summary>
public static class AppClaims
{
    public const string UserId = "sub";
    public const string Email = "email";
    public const string Name = "name";
    public const string Role = "role";
    public const string CampusId = "campus_id";
    public const string TeacherId = "teacher_id";

    /// <summary>Sello de seguridad: si cambia (contraseña, rol, desactivación), los tokens anteriores dejan de valer.</summary>
    public const string SecurityStamp = "stamp";
}
