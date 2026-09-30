namespace SmartTimetableGenerator.Application.Common.Security;

/// <summary>
/// Roles de la aplicación. Cada usuario tiene uno.
/// </summary>
public static class Roles
{
    /// <summary>Administra toda la institución: catálogos, usuarios, planes y horarios de todas las sedes.</summary>
    public const string Admin = "Admin";

    /// <summary>Gestiona el plan de estudios y los horarios de la sede que tiene asignada.</summary>
    public const string Coordinator = "Coordinador";

    /// <summary>Consulta los horarios publicados y el plan de estudios.</summary>
    public const string Teacher = "Docente";

    /// <summary>Para <see cref="AuthorizeAttribute.Roles"/>: administrador o coordinador.</summary>
    public const string Managers = Admin + "," + Coordinator;

    public static IReadOnlyList<string> All { get; } = [Admin, Coordinator, Teacher];
}
