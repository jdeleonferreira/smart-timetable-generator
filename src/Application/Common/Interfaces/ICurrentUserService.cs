namespace SmartTimetableGenerator.Application.Common.Interfaces;

/// <summary>
/// Usuario que hace la petición (tomado del token).
/// </summary>
public interface ICurrentUserService
{
    string? UserId { get; }

    bool IsAuthenticated => UserId is not null;

    IReadOnlyList<string> Roles => [];

    /// <summary>Sede asignada (coordinador).</summary>
    Guid? CampusId => null;

    /// <summary>Docente vinculado (docente).</summary>
    Guid? TeacherId => null;
}
