using Microsoft.AspNetCore.Identity;

namespace SmartTimetableGenerator.Infrastructure.Identity;

/// <summary>
/// Cuenta de usuario (ASP.NET Identity). El correo es también el nombre de usuario. Cada usuario tiene un rol.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    /// <summary>Sede que gestiona (coordinador).</summary>
    public Guid? CampusId { get; set; }

    /// <summary>Registro de docente vinculado (docente; opcional para el coordinador que también dicta clase).</summary>
    public Guid? TeacherId { get; set; }

    /// <summary>Un usuario inactivo no puede iniciar sesión y sus tokens dejan de valer.</summary>
    public bool IsActive { get; set; } = true;
}
