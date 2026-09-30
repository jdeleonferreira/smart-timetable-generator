using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartTimetableGenerator.Application;
using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Application.UseCases.Timetables.Generation;
using SmartTimetableGenerator.Domain.TimetableGeneration;
using SmartTimetableGenerator.Infrastructure.Persistence;
using SmartTimetableGenerator.Infrastructure.Persistence.Interceptors;
using SmartTimetableGenerator.Infrastructure.Scheduling;

namespace SmartTimetableGenerator.Scheduling.Tests.Common;

/// <summary>
/// Aplicación real (MediatR, validadores, servicio de generación, motor CP-SAT y configuraciones de EF Core)
/// sobre una base SQLite en memoria. Cada operación usa un ámbito y un DbContext nuevos, como en producción.
/// </summary>
public sealed class SchedulingTestHost : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;

    public SchedulingTestHost()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ICurrentUserService, TestUser>();
        services.AddScoped<EntitySaveChangesInterceptor>();
        services.AddDbContext<ApplicationDbContext>((sp, options) => options
            .UseSqlite(_connection)
            .AddInterceptors(sp.GetRequiredService<EntitySaveChangesInterceptor>()));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<ITimetableSolver, CpSatTimetableSolver>();
        services.AddApplication();

        _provider = services.BuildServiceProvider();

        using var scope = _provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreated();
    }

    /// <summary>Guarda datos en un contexto nuevo.</summary>
    public async Task SeedAsync(Action<IApplicationDbContext> seed)
    {
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        seed(db);
        await db.SaveChangesAsync();
    }

    /// <summary>Lee o modifica datos en un contexto nuevo (guarda los cambios al final).</summary>
    public async Task<T> WithDbAsync<T>(Func<IApplicationDbContext, Task<T>> action)
    {
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var result = await action(db);
        await db.SaveChangesAsync();
        return result;
    }

    public async Task RunJobAsync(GenerationJobId jobId)
    {
        using var scope = _provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ITimetableGenerationService>().RunAsync(jobId, CancellationToken.None);
    }

    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        using var scope = _provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
    }

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
    }

    private sealed class TestUser : ICurrentUserService
    {
        public string? UserId => "pruebas";
    }
}
