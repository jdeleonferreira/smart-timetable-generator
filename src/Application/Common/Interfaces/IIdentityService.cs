using SmartTimetableGenerator.Application.UseCases.Users.Common;

namespace SmartTimetableGenerator.Application.Common.Interfaces;

/// <summary>
/// Cuentas de usuario, contraseñas y emisión de tokens (implementado con ASP.NET Identity y JWT).
/// </summary>
public interface IIdentityService
{
    /// <summary>Verifica las credenciales y emite un token. Bloquea la cuenta tras varios intentos fallidos.</summary>
    Task<ErrorOr<AuthTokenDto>> LoginAsync(string email, string password, CancellationToken cancellationToken);

    Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken);

    Task<ErrorOr<UserDto>> GetUserAsync(string userId, CancellationToken cancellationToken);

    Task<ErrorOr<string>> CreateUserAsync(UserData data, string password, CancellationToken cancellationToken);

    Task<ErrorOr<Success>> UpdateUserAsync(string userId, UserData data, bool isActive, CancellationToken cancellationToken);

    /// <summary>El administrador fija una contraseña nueva (no pide la actual).</summary>
    Task<ErrorOr<Success>> ResetPasswordAsync(string userId, string newPassword, CancellationToken cancellationToken);

    Task<ErrorOr<Success>> ChangePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken);
}
