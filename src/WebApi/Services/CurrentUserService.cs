using System.Security.Claims;
using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Infrastructure.Identity;

namespace SmartTimetableGenerator.WebApi.Services;

/// <summary>
/// Usuario de la petición, tomado de los claims del token JWT.
/// </summary>
public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public string? UserId => User?.Identity?.IsAuthenticated == true ? User.FindFirstValue(AppClaims.UserId) : null;

    public bool IsAuthenticated => UserId is not null;

    public IReadOnlyList<string> Roles =>
        IsAuthenticated ? User!.FindAll(AppClaims.Role).Select(c => c.Value).ToList() : [];

    public Guid? CampusId => ReadGuid(AppClaims.CampusId);

    public Guid? TeacherId => ReadGuid(AppClaims.TeacherId);

    private Guid? ReadGuid(string claim) =>
        IsAuthenticated && Guid.TryParse(User!.FindFirstValue(claim), out var value) ? value : null;
}
