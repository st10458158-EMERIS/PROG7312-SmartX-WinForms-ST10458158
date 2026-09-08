using Microsoft.EntityFrameworkCore;
using SmartX.Api.Data;
using SmartX.Api.Dtos;

namespace SmartX.Api.Services;

// BackgroundService is used for a long-running, time-based telemetry simulation loop
// that runs independently of the desktop UI (Microsoft, 2025b).
public sealed class TelemetrySimulator(IServiceScopeFactory scopeFactory, ILogger<TelemetrySimulator> logger)
    : BackgroundService
{
    private readonly Random _random = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<SmartXDbContext>();
                var service = scope.ServiceProvider.GetRequiredService<TelemetryService>();
                var sensors = await db.Sensors.Where(s => s.IsEnabled).ToListAsync(stoppingToken);

                foreach (var sensor in sensors)
                {
                    var value = sensor.Metric switch
                    {
                        "Temperature" => 18 + _random.NextDouble() * 18,
                        "Humidity" => 35 + _random.NextDouble() * 55,
                        "Power Usage" => 120 + _random.NextDouble() * 420,
                        _ => _random.NextDouble() * 100
                    };

                    await service.IngestAsync(new TelemetryRequest(sensor.Id, Math.Round(value, 2)), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Telemetry simulator iteration failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
