using Microsoft.Extensions.DependencyInjection;
using SmartTimetableGenerator.Application.Common.Behaviours;
using SmartTimetableGenerator.Application.UseCases.Timetables.Generation;

namespace SmartTimetableGenerator.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var applicationAssembly = typeof(DependencyInjection).Assembly;

        services.AddValidatorsFromAssembly(applicationAssembly, includeInternalTypes: true);

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(applicationAssembly);
            config.AddOpenBehavior(typeof(UnhandledExceptionBehaviour<,>));
            config.AddOpenBehavior(typeof(AuthorizationBehaviour<,>));

            // NOTE: Switch to ValidationExceptionBehavior if you want to use exceptions over the result pattern for flow control
            //config.AddOpenBehavior(typeof(ValidationExceptionBehaviour<,>));
            config.AddOpenBehavior(typeof(ValidationErrorOrResultBehavior<,>));

            config.AddOpenBehavior(typeof(PerformanceBehaviour<,>));
        });

        services.AddScoped<ITimetableGenerationService, TimetableGenerationService>();

        return services;
    }
}