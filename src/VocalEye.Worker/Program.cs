var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<HeartbeatWorker>();
builder.Build().Run();

// Placeholder: replaced by ingestion / long-running agent jobs (see src/VocalEye.Worker/Jobs).
public sealed class HeartbeatWorker(ILogger<HeartbeatWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Worker alive at {Time}", DateTimeOffset.UtcNow);
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
