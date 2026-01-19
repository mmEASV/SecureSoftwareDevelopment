using Microsoft.Extensions.Options;

namespace ClientPortal.Api.Services;

/// <summary>
/// Background service that periodically syncs releases from Admin.Api through Cloudflare tunnel
/// and responds to immediate sync triggers from webhooks
/// </summary>
public class ReleaseSyncService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReleaseSyncService> _logger;
    private readonly SyncTriggerService _syncTrigger;
    private readonly SyncConfiguration _config;

    public ReleaseSyncService(
        IServiceProvider serviceProvider,
        ILogger<ReleaseSyncService> logger,
        SyncTriggerService syncTrigger,
        IOptions<SyncConfiguration> config)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _syncTrigger = syncTrigger;
        _config = config.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Release Sync Service started. Sync interval: {Interval} minutes", _config.SyncIntervalMinutes);

        // Initial delay to let the application start
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        // Run both scheduled and triggered sync tasks concurrently
        var scheduledTask = RunScheduledSyncAsync(stoppingToken);
        var triggeredTask = RunTriggeredSyncAsync(stoppingToken);

        await Task.WhenAll(scheduledTask, triggeredTask);
    }

    /// <summary>
    /// Runs the scheduled sync on a fixed interval
    /// </summary>
    private async Task RunScheduledSyncAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExecuteSyncWithScopeAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during scheduled release sync");
            }

            // Wait for the configured interval before next sync
            await Task.Delay(TimeSpan.FromMinutes(_config.SyncIntervalMinutes), stoppingToken);
        }
    }

    /// <summary>
    /// Listens for sync triggers from the channel (webhook notifications)
    /// </summary>
    private async Task RunTriggeredSyncAsync(CancellationToken stoppingToken)
    {
        await foreach (var request in _syncTrigger.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                _logger.LogInformation("Processing triggered sync request for release {ReleaseId} (requested at {RequestedAt})",
                    request.ReleaseId, request.RequestedAt);

                await ExecuteSyncWithScopeAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during triggered release sync for {ReleaseId}", request.ReleaseId);
            }
        }
    }

    /// <summary>
    /// Creates a scope and executes the sync using the executor
    /// </summary>
    private async Task ExecuteSyncWithScopeAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var executor = scope.ServiceProvider.GetRequiredService<IReleaseSyncExecutor>();
        var result = await executor.ExecuteSyncAsync(cancellationToken);

        if (result.Success)
        {
            if (result.NewReleasesCount > 0)
            {
                _logger.LogInformation("Sync completed: {Count} new releases synced", result.NewReleasesCount);
            }
        }
        else
        {
            _logger.LogWarning("Sync failed: {ErrorMessage}", result.ErrorMessage);
        }
    }
}

/// <summary>
/// Configuration for release synchronization
/// </summary>
public class SyncConfiguration
{
    public string AdminApiUrl { get; set; } = string.Empty;
    public int SyncIntervalMinutes { get; set; } = 5; // Default: sync every 5 minutes
}

/// <summary>
/// DTO matching the sync endpoint response from Admin.Api
/// </summary>
public class ReleaseSyncDto
{
    public Guid ReleaseId { get; set; }
    public Guid UpdateId { get; set; }
    public string Version { get; set; } = string.Empty;
    public DateTime ReleaseDate { get; set; }
    public bool IsMandatory { get; set; }
    public int MaxPostponeDays { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string Changelog { get; set; } = string.Empty;
    public string CVEList { get; set; } = string.Empty;
    public string FileHash { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
}
