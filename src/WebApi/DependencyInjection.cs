using System.Text.Json.Serialization;
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

        // Enums como texto en la API (ej.: "Regular", "CounterShift") en lugar de números
        services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddOpenApi();

        services.AddHealthChecks(config);
    }
}
// #pragma warning restore IDE0055