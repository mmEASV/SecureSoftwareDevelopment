using Admin.Shared.Models;
using ClientPortal.Api.Domain.Interfaces;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ClientPortal.Api.Services;

/// <summary>
/// Executes release synchronization from Admin.Api
/// Contains the extracted sync logic from ReleaseSyncService with thread-safety
/// </summary>
public class ReleaseSyncExecutor : IReleaseSyncExecutor
{
    private readonly IReleaseRepository _releaseRepository;
    private readonly IUpdateRepository _updateRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SyncConfiguration _config;
    private readonly ILogger<ReleaseSyncExecutor> _logger;

    // Semaphore to prevent concurrent sync operations
    private static readonly SemaphoreSlim _syncLock = new(1, 1);

    public ReleaseSyncExecutor(
        IReleaseRepository releaseRepository,
        IUpdateRepository updateRepository,
        IHttpClientFactory httpClientFactory,
        IOptions<SyncConfiguration> config,
        ILogger<ReleaseSyncExecutor> logger)
    {
        _releaseRepository = releaseRepository;
        _updateRepository = updateRepository;
        _httpClientFactory = httpClientFactory;
        _config = config.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SyncResult> ExecuteSyncAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_config.AdminApiUrl))
        {
            _logger.LogWarning("AdminApiUrl not configured. Skipping sync.");
            return new SyncResult(false, 0, "AdminApiUrl not configured");
        }

        // Try to acquire the lock without waiting - if already syncing, skip
        if (!await _syncLock.WaitAsync(0, cancellationToken))
        {
            _logger.LogInformation("Sync already in progress, skipping this request");
            return new SyncResult(true, 0, "Sync already in progress");
        }

        try
        {
            return await ExecuteSyncInternalAsync(cancellationToken);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    private async Task<SyncResult> ExecuteSyncInternalAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting release sync from {AdminApiUrl}", _config.AdminApiUrl);

        try
        {
            // Fetch active releases from Admin.Api
            var httpClient = _httpClientFactory.CreateClient("AdminApi");
            var response = await httpClient.GetAsync("/api/sync/releases/active", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch releases from Admin.Api. Status: {Status}", response.StatusCode);
                return new SyncResult(false, 0, $"Failed to fetch releases: {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var syncDtos = JsonSerializer.Deserialize<List<ReleaseSyncDto>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (syncDtos == null || syncDtos.Count == 0)
            {
                _logger.LogInformation("No active releases to sync");
                return new SyncResult(true, 0);
            }

            _logger.LogInformation("Fetched {Count} active releases from Admin.Api", syncDtos.Count);

            // Sync each release
            int syncedCount = 0;
            foreach (var dto in syncDtos)
            {
                try
                {
                    // Check if update exists in ClientDb
                    var existingUpdate = await _updateRepository.GetByIdAsync(dto.UpdateId, cancellationToken);
                    if (existingUpdate == null)
                    {
                        // Create cached Update record in ClientDb
                        var update = new Update
                        {
                            Id = dto.UpdateId,
                            Version = dto.Version,
                            Title = $"Update {dto.Version}",
                            Description = dto.Changelog,
                            ChangeLog = dto.Changelog,
                            SecurityFixes = string.IsNullOrEmpty(dto.CVEList)
                                ? new List<string>()
                                : dto.CVEList.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList(),
                            FilePath = $"updates/{dto.FileName}",
                            FileHash = dto.FileHash,
                            DigitalSignature = dto.Signature,
                            FileSize = dto.FileSizeBytes,
                            UpdateType = Admin.Shared.Enums.UpdateType.Feature,
                            Severity = Enum.Parse<Admin.Shared.Enums.UpdateSeverity>(dto.Severity, true),
                            IsSecurityUpdate = !string.IsNullOrEmpty(dto.CVEList),
                            TargetDeviceTypes = new List<string>(),
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = Guid.Empty,
                            IsActive = true
                        };

                        await _updateRepository.CreateAsync(update, cancellationToken);
                        _logger.LogInformation("Created update {UpdateId} - Version {Version}", dto.UpdateId, dto.Version);
                    }

                    // Check if release exists in ClientDb
                    var existingRelease = await _releaseRepository.GetByIdAsync(dto.ReleaseId, cancellationToken);
                    if (existingRelease == null)
                    {
                        // Create cached Release record in ClientDb
                        var release = new Release
                        {
                            Id = dto.ReleaseId,
                            UpdateId = dto.UpdateId,
                            ReleaseDate = dto.ReleaseDate,
                            IsActive = true,
                            IsMandatory = dto.IsMandatory,
                            MinimumVersion = null,
                            MaxPostponeDays = dto.MaxPostponeDays,
                            ReleaseNotes = dto.Changelog,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = Guid.Empty
                        };

                        await _releaseRepository.CreateAsync(release, cancellationToken);
                        _logger.LogInformation("Created release {ReleaseId} - Version {Version} - Severity: {Severity}",
                            dto.ReleaseId, dto.Version, dto.Severity);
                        syncedCount++;
                    }
                    else
                    {
                        _logger.LogDebug("Release {ReleaseId} already exists in ClientDb", dto.ReleaseId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error syncing release {ReleaseId}", dto.ReleaseId);
                }
            }

            _logger.LogInformation("Release sync completed successfully. Synced {SyncedCount} new releases", syncedCount);
            return new SyncResult(true, syncedCount);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error connecting to Admin.Api at {Url}", _config.AdminApiUrl);
            return new SyncResult(false, 0, $"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during release sync");
            return new SyncResult(false, 0, $"Unexpected error: {ex.Message}");
        }
    }
}
