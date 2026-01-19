namespace ClientPortal.Api.Services;

/// <summary>
/// Service for triggering release synchronization from external sources (e.g., webhooks)
/// </summary>
public interface ISyncTriggerService
{
    /// <summary>
    /// Trigger an immediate sync operation
    /// </summary>
    /// <param name="releaseId">Optional release ID that triggered the sync (for logging)</param>
    /// <returns>True if the trigger was successfully queued, false if the queue was full</returns>
    bool TriggerSync(Guid? releaseId = null);
}
