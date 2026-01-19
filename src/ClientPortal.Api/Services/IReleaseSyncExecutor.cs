namespace ClientPortal.Api.Services;

/// <summary>
/// Interface for executing release synchronization from Admin.Api
/// </summary>
public interface IReleaseSyncExecutor
{
    /// <summary>
    /// Execute a sync operation to fetch and store releases from Admin.Api
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the sync operation</returns>
    Task<SyncResult> ExecuteSyncAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a sync operation
/// </summary>
/// <param name="Success">Whether the sync completed successfully</param>
/// <param name="NewReleasesCount">Number of new releases synced</param>
/// <param name="ErrorMessage">Error message if the sync failed</param>
public record SyncResult(bool Success, int NewReleasesCount, string? ErrorMessage = null);
