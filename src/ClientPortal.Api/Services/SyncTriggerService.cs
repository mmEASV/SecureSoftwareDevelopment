using System.Threading.Channels;

namespace ClientPortal.Api.Services;

/// <summary>
/// Request payload for triggering a sync operation
/// </summary>
public record SyncRequest(Guid? ReleaseId, DateTime RequestedAt);

/// <summary>
/// Channel-based sync trigger service that allows webhook endpoints to signal
/// the background service to sync immediately
/// </summary>
public class SyncTriggerService : ISyncTriggerService
{
    private readonly Channel<SyncRequest> _channel;
    private readonly ILogger<SyncTriggerService> _logger;

    /// <summary>
    /// Reader for consuming sync requests (used by the background service)
    /// </summary>
    public ChannelReader<SyncRequest> Reader => _channel.Reader;

    public SyncTriggerService(ILogger<SyncTriggerService> logger)
    {
        _logger = logger;

        // BoundedChannel with capacity 1 + DropOldest provides natural debouncing:
        // - If multiple webhooks arrive rapidly, only the latest triggers a sync
        // - Prevents queue buildup if syncs are slow
        _channel = Channel.CreateBounded<SyncRequest>(new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false // Multiple webhook requests can trigger concurrently
        });
    }

    /// <inheritdoc />
    public bool TriggerSync(Guid? releaseId = null)
    {
        var request = new SyncRequest(releaseId, DateTime.UtcNow);

        if (_channel.Writer.TryWrite(request))
        {
            _logger.LogInformation("Sync triggered for release {ReleaseId}", releaseId);
            return true;
        }

        // This should rarely happen with DropOldest mode, but handle it gracefully
        _logger.LogWarning("Failed to queue sync trigger for release {ReleaseId} - channel full", releaseId);
        return false;
    }
}
