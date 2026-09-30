using MediatR;
using SmartTimetableGenerator.Application.UseCases.Auth.Commands.ChangePassword;
using SmartTimetableGenerator.Application.UseCases.Auth.Commands.Login;
using SmartTimetableGenerator.Application.UseCases.Auth.Queries.GetCurrentUser;
using SmartTimetableGenerator.Application.UseCases.Users.Commands.CreateUser;
using SmartTimetableGenerator.Application.UseCases.Users.Commands.ResetUserPassword;
using SmartTimetableGenerator.Application.UseCases.Users.Commands.UpdateUser;
using SmartTimetableGenerator.Application.UseCases.Users.Common;
using SmartTimetableGenerator.Application.UseCases.Users.Queries.GetUsers;
using SmartTimetableGenerator.WebApi.Extensions;

namespace SmartTimetableGenerator.WebApi.Endpoints;

/// <summary>
/// Inicio de sesión, usuario actual y administración de usuarios.
/// </summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var auth = app.MapApiGroup("auth");

        auth
            .MapPost("/login", async (ISender sender, LoginCommand command, CancellationToken ct) =>
            {
                var result = await sender.Send(command, ct);
                return result.Match(token => Results.Ok(token), CustomResult.Problem);
            })
            .AllowAnonymous()
            .WithName("Login")
            .WithSummary("Inicia sesión. Envíe el accessToken en Authorization: Bearer … en las demás peticiones")
            .Produces<AuthTokenDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        auth
            .MapGet("/me", async (ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetCurrentUserQuery(), ct);
                return result.Match(user => Results.Ok(user), CustomResult.Problem);
            })
            .WithName("GetCurrentUser")
            .WithSummary("Usuario que inició sesión: rol, sede o docente vinculado")
            .ProducesGet<UserDto>();

        auth
            .MapPost("/change-password", async (ISender sender, ChangePasswordCommand command, CancellationToken ct) =>
            {
                var result = await sender.Send(command, ct);
                return result.Match(_ => Results.NoContent(), CustomResult.Problem);
            })
            .WithName("ChangePassword")
            .WithSummary("Cambia la contraseña propia")
            .ProducesPut();

        var users = app.MapApiGroup("users");

        users
            .MapGet("/", async (ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetUsersQuery(), ct);
                return result.Match(list => Results.Ok(list), CustomResult.Problem);
            })
            .WithName("GetUsers")
            .WithSummary("Usuarios (solo administrador)")
            .ProducesGet<UserDto[]>()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        users
            .MapPost("/", async (ISender sender, CreateUserCommand command, CancellationToken ct) =>
            {
                var result = await sender.Send(command, ct);
                return result.Match(id => Results.Created($"/api/users/{id}", new { id }), CustomResult.Problem);
            })
            .WithName("CreateUser")
            .WithSummary("Crea un usuario: role = Admin, Coordinador (con campusId) o Docente (con teacherId)")
            .ProducesPost()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        users
            .MapPut("/{userId}", async (ISender sender, string userId, UpdateUserCommand command, CancellationToken ct) =>
            {
                var result = await sender.Send(command with { UserId = userId }, ct);
                return result.Match(_ => Results.NoContent(), CustomResult.Problem);
            })
            .WithName("UpdateUser")
            .WithSummary("Cambia datos, rol o estado (isActive) de un usuario; sus sesiones abiertas se cierran si cambian sus permisos")
            .ProducesPut()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        users
            .MapPost("/{userId}/reset-password", async (ISender sender, string userId, ResetUserPasswordCommand command, CancellationToken ct) =>
            {
                var result = await sender.Send(command with { UserId = userId }, ct);
                return result.Match(_ => Results.NoContent(), CustomResult.Problem);
            })
            .WithName("ResetUserPassword")
            .WithSummary("Fija una contraseña nueva para un usuario y lo desbloquea")
            .ProducesPut()
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
