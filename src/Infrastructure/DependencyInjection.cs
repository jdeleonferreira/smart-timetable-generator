using EntityFramework.Exceptions.SqlServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Infrastructure.Persistence;
using SmartTimetableGenerator.Infrastructure.BackgroundJobs;
using SmartTimetableGenerator.Infrastructure.Documents;
using SmartTimetableGenerator.Infrastructure.Identity;
using SmartTimetableGenerator.Infrastructure.Persistence.Interceptors;
using SmartTimetableGenerator.Infrastructure.Scheduling;

namespace SmartTimetableGenerator.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.AddSqlServerDbContext<ApplicationDbContext>("SmartTimetable",
            null,
            options =>
            {
                var serviceProvider = builder.Services.BuildServiceProvider();
                options.AddInterceptors(
                    serviceProvider.GetRequiredService<EntitySaveChangesInterceptor>(),
                    serviceProvider.GetRequiredService<DispatchDomainEventsInterceptor>());

                // Return strongly typed useful exceptions
                options.UseExceptionProcessor();
            });

        var services = builder.Services;

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<EntitySaveChangesInterceptor>();
        services.AddScoped<DispatchDomainEventsInterceptor>();

        services.AddSingleton(TimeProvider.System);

        // Documentos en Word y PDF
        services.AddSingleton<IDocumentRenderer, DocumentRenderer>();

        // Usuarios, roles y tokens JWT
        services.AddJwtAuthentication(builder.Configuration);

        // Generación de horarios: motor CP-SAT y trabajador en segundo plano
        services.AddScoped<ITimetableSolver, CpSatTimetableSolver>();
        services.AddHostedService<GenerationJobWorker>();
    }
}