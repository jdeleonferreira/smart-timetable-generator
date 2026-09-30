using SmartTimetableGenerator.Application.Common.Interfaces;
using System.Security.Claims;

namespace SmartTimetableGenerator.WebApi.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public string? UserId => httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
}