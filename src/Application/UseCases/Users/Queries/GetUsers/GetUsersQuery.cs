using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Application.Common.Security;
using SmartTimetableGenerator.Application.UseCases.Users.Common;

namespace SmartTimetableGenerator.Application.UseCases.Users.Queries.GetUsers;

/// <summary>
/// Usuarios de la aplicación, ordenados por nombre.
/// </summary>
[Authorize(Roles = Roles.Admin)]
public sealed record GetUsersQuery : IRequest<ErrorOr<IReadOnlyList<UserDto>>>;

internal sealed class GetUsersQueryHandler(IIdentityService identityService)
    : IRequestHandler<GetUsersQuery, ErrorOr<IReadOnlyList<UserDto>>>
{
    public async Task<ErrorOr<IReadOnlyList<UserDto>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await identityService.GetUsersAsync(cancellationToken);
        return users.ToList();
    }
}
