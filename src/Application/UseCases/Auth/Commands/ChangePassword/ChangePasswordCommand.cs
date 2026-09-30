using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Application.Common.Security;
using SmartTimetableGenerator.Application.UseCases.Users.Common;

namespace SmartTimetableGenerator.Application.UseCases.Auth.Commands.ChangePassword;

/// <summary>
/// El usuario cambia su propia contraseña.
/// </summary>
[Authorize]
public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest<ErrorOr<Success>>;

internal sealed class ChangePasswordCommandHandler(IIdentityService identityService, ICurrentUserService currentUser)
    : IRequestHandler<ChangePasswordCommand, ErrorOr<Success>>
{
    public Task<ErrorOr<Success>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken) =>
        identityService.ChangePasswordAsync(currentUser.UserId!, request.CurrentPassword, request.NewPassword, cancellationToken);
}

internal sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(v => v.CurrentPassword).NotEmpty();
        RuleFor(v => v.NewPassword).NotEmpty().MinimumLength(UserRules.PasswordMinLength);
    }
}
