using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Application.Common.Security;
using SmartTimetableGenerator.Application.UseCases.Users.Common;

namespace SmartTimetableGenerator.Application.UseCases.Auth.Queries.GetCurrentUser;

/// <summary>
/// Datos del usuario que inició sesión (rol, sede o docente vinculado).
/// </summary>
[Authorize]
public sealed record GetCurrentUserQuery : IRequest<ErrorOr<UserDto>>;

internal sealed class GetCurrentUserQueryHandler(IIdentityService identityService, ICurrentUserService currentUser)
    : IRequestHandler<GetCurrentUserQuery, ErrorOr<UserDto>>
{
    public Task<ErrorOr<UserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken) =>
        identityService.GetUserAsync(currentUser.UserId!, cancellationToken);
}
