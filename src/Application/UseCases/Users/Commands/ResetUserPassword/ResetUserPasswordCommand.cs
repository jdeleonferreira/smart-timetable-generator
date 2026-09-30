using System.Text.Json.Serialization;
using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Application.Common.Security;
using SmartTimetableGenerator.Application.UseCases.Users.Common;

namespace SmartTimetableGenerator.Application.UseCases.Users.Commands.ResetUserPassword;

/// <summary>
/// El administrador fija una contraseña nueva para un usuario (por ejemplo, si la olvidó) y lo desbloquea.
/// </summary>
[Authorize(Roles = Roles.Admin)]
public sealed record ResetUserPasswordCommand(string NewPassword) : IRequest<ErrorOr<Success>>
{
    [JsonIgnore]
    public string UserId { get; set; } = string.Empty;
}

internal sealed class ResetUserPasswordCommandHandler(IIdentityService identityService)
    : IRequestHandler<ResetUserPasswordCommand, ErrorOr<Success>>
{
    public Task<ErrorOr<Success>> Handle(ResetUserPasswordCommand request, CancellationToken cancellationToken) =>
        identityService.ResetPasswordAsync(request.UserId, request.NewPassword, cancellationToken);
}

internal sealed class ResetUserPasswordCommandValidator : AbstractValidator<ResetUserPasswordCommand>
{
    public ResetUserPasswordCommandValidator()
    {
        RuleFor(v => v.UserId).NotEmpty();
        RuleFor(v => v.NewPassword).NotEmpty().MinimumLength(UserRules.PasswordMinLength);
    }
}
