using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Application.UseCases.Users.Common;

namespace SmartTimetableGenerator.Application.UseCases.Auth.Commands.Login;

/// <summary>
/// Inicia sesión con correo y contraseña. Devuelve el token (JWT) que se envía en
/// <c>Authorization: Bearer …</c> en las demás peticiones.
/// </summary>
public sealed record LoginCommand(string Email, string Password) : IRequest<ErrorOr<AuthTokenDto>>;

internal sealed class LoginCommandHandler(IIdentityService identityService)
    : IRequestHandler<LoginCommand, ErrorOr<AuthTokenDto>>
{
    public Task<ErrorOr<AuthTokenDto>> Handle(LoginCommand request, CancellationToken cancellationToken) =>
        identityService.LoginAsync(request.Email.Trim(), request.Password, cancellationToken);
}

internal sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(v => v.Email).NotEmpty().MaximumLength(UserRules.EmailMaxLength);
        RuleFor(v => v.Password).NotEmpty();
    }
}
