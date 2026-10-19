namespace VocalEye.Worker.Jobs;

/// <summary>Placeholder job, replaced by ingestion and long-running agent jobs.</summary>
public sealed class HeartbeatWorker : BackgroundService
{
    private readonly ILogger<HeartbeatWorker> _logger;

    public HeartbeatWorker(ILogger<HeartbeatWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker alive at {Time}", DateTimeOffset.UtcNow);
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
