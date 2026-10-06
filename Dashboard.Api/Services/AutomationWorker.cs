using Dashboard.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Api.Services;

public sealed class AutomationWorker(IServiceScopeFactory scopeFactory, ILogger<AutomationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueAsync(stoppingToken);
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Falló el ciclo local de automatizaciones.");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }

    private async Task ProcessDueAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DashboardDbContext>();
        var executor = scope.ServiceProvider.GetRequiredService<AutomationExecutor>();
        var now = DateTimeOffset.UtcNow;
        var due = (await db.AutomationSchedules
            .Where(item => item.IsEnabled && item.NextRunAt != null).ToListAsync(cancellationToken))
            .Where(item => item.NextRunAt <= now).OrderBy(item => item.NextRunAt).Take(10).ToList();
        foreach (var schedule in due)
        {
            await executor.RunAsync(schedule, "scheduled", cancellationToken);
        }
    }
}
