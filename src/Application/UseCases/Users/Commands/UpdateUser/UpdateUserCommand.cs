using System.Text.Json.Serialization;
using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Application.Common.Security;
using SmartTimetableGenerator.Application.UseCases.Users.Common;

namespace SmartTimetableGenerator.Application.UseCases.Users.Commands.UpdateUser;

/// <summary>
/// Cambia los datos, el rol o el estado de un usuario. Un usuario inactivo no puede iniciar sesión.
/// Siempre debe quedar al menos un administrador activo.
/// </summary>
[Authorize(Roles = Roles.Admin)]
public sealed record UpdateUserCommand(
    string Email,
    string FullName,
    string Role,
    Guid? CampusId = null,
    Guid? TeacherId = null,
    bool IsActive = true) : IRequest<ErrorOr<Success>>
{
    [JsonIgnore]
    public string UserId { get; set; } = string.Empty;
}

internal sealed class UpdateUserCommandHandler(IApplicationDbContext dbContext, IIdentityService identityService)
    : IRequestHandler<UpdateUserCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var data = await UserReferences.NormalizeAsync(dbContext,
            new UserData(request.Email, request.FullName, request.Role, request.CampusId, request.TeacherId), cancellationToken);
        if (data.IsError)
            return data.Errors;

        return await identityService.UpdateUserAsync(request.UserId, data.Value, request.IsActive, cancellationToken);
    }
}

internal sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(v => v.UserId).NotEmpty();
        RuleFor(v => v.Email).NotEmpty().EmailAddress().MaximumLength(UserRules.EmailMaxLength);
        RuleFor(v => v.FullName).NotEmpty().MaximumLength(UserRules.FullNameMaxLength);
        RuleFor(v => v.Role).NotEmpty();
    }
}
