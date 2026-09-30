namespace SmartTimetableGenerator.Application.UseCases.Users.Common;

/// <summary>
/// Usuario de la aplicación. Coordinador: <see cref="CampusId"/> es su sede. Docente: <see cref="TeacherId"/> es su registro de docente.
/// </summary>
public sealed record UserDto(
    string Id,
    string Email,
    string FullName,
    string Role,
    Guid? CampusId,
    Guid? TeacherId,
    bool IsActive);

/// <summary>Datos editables de un usuario.</summary>
public sealed record UserData(string Email, string FullName, string Role, Guid? CampusId, Guid? TeacherId);

public sealed record AuthTokenDto(string AccessToken, string TokenType, DateTimeOffset ExpiresAt, UserDto User);
