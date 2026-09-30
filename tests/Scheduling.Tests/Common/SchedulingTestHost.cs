using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartTimetableGenerator.Application;
using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Application.UseCases.Timetables.Generation;
using SmartTimetableGenerator.Domain.Campuses;
using SmartTimetableGenerator.Domain.Teachers;
using SmartTimetableGenerator.Domain.TimetableGeneration;
using SmartTimetableGenerator.Infrastructure.Identity;
using SmartTimetableGenerator.Infrastructure.Persistence;
using SmartTimetableGenerator.Infrastructure.Persistence.Interceptors;
using SmartTimetableGenerator.Infrastructure.Scheduling;

namespace SmartTimetableGenerator.Scheduling.Tests.Common;

/// <summary>
/// Aplicación real (MediatR, validadores, permisos, usuarios con ASP.NET Identity, servicio de generación,
/// motor CP-SAT y configuraciones de EF Core) sobre una base SQLite en memoria.
/// Cada operación usa un ámbito y un DbContext nuevos, como en producción.
/// Las peticiones se hacen como administrador salvo que se cambie <see cref="User"/> (ver <see cref="SignInAs"/>).
/// </summary>
public sealed class SchedulingTestHost : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;

    public const string JwtKey = "llave-de-pruebas-con-al-menos-treinta-y-dos-caracteres";

    /// <summary>Usuario con el que se envían las peticiones.</summary>
    public TestUser User { get; } = new();

    public SchedulingTestHost()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(User);
        services.AddScoped<ICurrentUserService>(sp => sp.GetRequiredService<TestUser>());
        services.AddIdentityServices(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:SigningKey"] = JwtKey })
            .Build());
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

    /// <summary>Las peticiones siguientes se hacen con este usuario (null = sin sesión).</summary>
    public void SignInAs(string? role, CampusId? campusId = null, TeacherId? teacherId = null)
    {
        User.UserId = role is null ? null : $"usuario-{role.ToLowerInvariant()}";
        User.Roles = role is null ? [] : [role];
        User.CampusId = campusId?.Value;
        User.TeacherId = teacherId?.Value;
    }

    public T GetService<T>(IServiceScope scope) where T : notnull => scope.ServiceProvider.GetRequiredService<T>();

    public IServiceScope CreateScope() => _provider.CreateScope();

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

    public sealed class TestUser : ICurrentUserService
    {
        public string? UserId { get; set; } = "pruebas";

        public IReadOnlyList<string> Roles { get; set; } = [Application.Common.Security.Roles.Admin];

        public Guid? CampusId { get; set; }

        public Guid? TeacherId { get; set; }
    }
}
