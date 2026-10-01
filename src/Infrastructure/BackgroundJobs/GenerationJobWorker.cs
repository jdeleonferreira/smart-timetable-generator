using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartTimetableGenerator.Application.UseCases.Timetables.Generation;
using SmartTimetableGenerator.Domain.TimetableGeneration;
using SmartTimetableGenerator.Infrastructure.Persistence;

namespace SmartTimetableGenerator.Infrastructure.BackgroundJobs;

/// <summary>
/// Procesa en segundo plano las solicitudes de generación en cola, una a la vez.
/// Al iniciar, marca como fallidas las que quedaron "en ejecución" por un reinicio del servidor.
/// </summary>
public sealed class GenerationJobWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<GenerationJobWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RecoverInterruptedJobsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await ProcessNextAsync(stoppingToken);
                if (!processed)
                    await Task.Delay(PollInterval, timeProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
#pragma warning disable CA1031 // El trabajador no debe detenerse por un error puntual
            catch (Exception ex)
#pragma warning restore CA1031
            {
                logger.LogError(ex, "Error procesando solicitudes de generación");
                await Task.Delay(PollInterval, timeProvider, stoppingToken);
            }
        }
    }

    private async Task<bool> ProcessNextAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var next = await db.GenerationJobs
            .AsNoTracking()
            .Where(j => j.Status == GenerationJobStatus.Queued)
            .OrderBy(j => j.RequestedAt)
            .FirstOrDefaultAsync(ct);

        if (next is null)
            return false;

        logger.LogInformation("Generando horario (solicitud {JobId})", next.Id);
        var service = scope.ServiceProvider.GetRequiredService<ITimetableGenerationService>();
        await service.RunAsync(next.Id, ct);
        return true;
    }

    private async Task RecoverInterruptedJobsAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var running = await db.GenerationJobs.Where(j => j.Status == GenerationJobStatus.Running).ToListAsync(ct);

            foreach (var job in running)
                job.Fail(timeProvider.GetUtcNow(), "La generación se interrumpió porque el servidor se reinició. Vuelva a solicitarla.");

            if (running.Count > 0)
                await db.SaveChangesAsync(ct);
        }
#pragma warning disable CA1031 // La base de datos puede no estar lista todavía; se reintenta en el ciclo normal
        catch (Exception ex)
#pragma warning restore CA1031
        {
            logger.LogWarning(ex, "No se pudieron revisar las solicitudes interrumpidas");
        }
    }
}
