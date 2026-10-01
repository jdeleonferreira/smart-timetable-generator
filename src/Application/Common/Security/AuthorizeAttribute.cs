namespace SmartTimetableGenerator.Application.Common.Security;

/// <summary>
/// Marca un comando o consulta que exige un usuario autenticado y, si se indican, alguno de los roles
/// (separados por coma). Lo aplica <c>AuthorizationBehaviour</c>; el request debe devolver <c>ErrorOr</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class AuthorizeAttribute : Attribute
{
    public string Roles { get; init; } = string.Empty;
}
