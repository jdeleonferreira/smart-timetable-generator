using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.WebApi.HealthChecks;
using SmartTimetableGenerator.WebApi.Services;

namespace SmartTimetableGenerator.WebApi;
// TODO: Can we remove this?
// #pragma warning disable IDE0055

public static class DependencyInjection
{
    public static void AddWebApi(this IServiceCollection services, IConfiguration config)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddOpenApi();

        services.AddHealthChecks(config);
    }
}
// #pragma warning restore IDE0055