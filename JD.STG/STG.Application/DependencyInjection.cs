using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STG.Application.Services;

namespace STG.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration config)
    {

        services.AddScoped<CurriculumService>();
        services.AddScoped<ResourceService>();
        services.AddScoped<SchedulerService>();


        return services;
    }
}
