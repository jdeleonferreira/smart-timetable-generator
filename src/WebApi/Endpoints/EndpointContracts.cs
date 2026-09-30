namespace SmartTimetableGenerator.WebApi.Endpoints;

/// <summary>Respuesta 201 con el id del recurso creado.</summary>
public sealed record CreatedIdDto(Guid Id);

/// <summary>Respuesta 201 con el id del usuario creado.</summary>
public sealed record CreatedUserDto(string Id);

/// <summary>Respuesta 202 con el id de la solicitud de generación.</summary>
public sealed record GenerationQueuedDto(Guid JobId);
