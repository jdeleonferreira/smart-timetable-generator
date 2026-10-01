using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Application.Common.Security;
using SmartTimetableGenerator.Application.UseCases.Users.Common;

namespace SmartTimetableGenerator.Application.UseCases.Users.Commands.CreateUser;

/// <summary>
/// Crea un usuario con su rol: Admin, Coordinador (con su sede) o Docente (vinculado a su registro de docente).
/// </summary>
[Authorize(Roles = Roles.Admin)]
public sealed record CreateUserCommand(
    string Email,
    string FullName,
    string Role,
    string Password,
    Guid? CampusId = null,
    Guid? TeacherId = null) : IRequest<ErrorOr<string>>;

internal sealed class CreateUserCommandHandler(IApplicationDbContext dbContext, IIdentityService identityService)
    : IRequestHandler<CreateUserCommand, ErrorOr<string>>
{
    public async Task<ErrorOr<string>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var data = await UserReferences.NormalizeAsync(dbContext,
            new UserData(request.Email, request.FullName, request.Role, request.CampusId, request.TeacherId), cancellationToken);
        if (data.IsError)
            return data.Errors;

        return await identityService.CreateUserAsync(data.Value, request.Password, cancellationToken);
    }
}

internal sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(v => v.Email).NotEmpty().EmailAddress().MaximumLength(UserRules.EmailMaxLength);
        RuleFor(v => v.FullName).NotEmpty().MaximumLength(UserRules.FullNameMaxLength);
        RuleFor(v => v.Role).NotEmpty();
        RuleFor(v => v.Password).NotEmpty().MinimumLength(UserRules.PasswordMinLength);
    }
}
