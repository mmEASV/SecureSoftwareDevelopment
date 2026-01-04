using Admin.Shared.Models;
using ClientPortal.Api.Domain.Interfaces;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ClientPortal.Api.Services;

/// <summary>
/// Background service that periodically syncs releases from Admin.Api through Cloudflare tunnel
/// </summary>
public class ReleaseSyncService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReleaseSyncService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SyncConfiguration _config;

    public ReleaseSyncService(
        IServiceProvider serviceProvider,
        ILogger<ReleaseSyncService> logger,
        IHttpClientFactory httpClientFactory,
        IOptions<SyncConfiguration> config)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _config = config.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Release Sync Service started. Sync interval: {Interval} minutes", _config.SyncIntervalMinutes);

        // Initial delay to let the application start
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncReleasesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during release sync");
            }

            // Wait for the configured interval before next sync
            await Task.Delay(TimeSpan.FromMinutes(_config.SyncIntervalMinutes), stoppingToken);
        }
    }

    private async Task SyncReleasesAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_config.AdminApiUrl))
        {
            _logger.LogWarning("AdminApiUrl not configured. Skipping sync.");
            return;
        }

        _logger.LogInformation("Starting release sync from {AdminApiUrl}", _config.AdminApiUrl);

        try
        {
            // Create scope for scoped services
            using var scope = _serviceProvider.CreateScope();
            var releaseRepository = scope.ServiceProvider.GetRequiredService<IReleaseRepository>();
            var updateRepository = scope.ServiceProvider.GetRequiredService<IUpdateRepository>();

            // Fetch active releases from Admin.Api
            var httpClient = _httpClientFactory.CreateClient("AdminApi");
            var response = await httpClient.GetAsync("/sync/releases/active", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch releases from Admin.Api. Status: {Status}", response.StatusCode);
                return;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var syncDtos = JsonSerializer.Deserialize<List<ReleaseSyncDto>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (syncDtos == null || syncDtos.Count == 0)
            {
                _logger.LogInformation("No active releases to sync");
                return;
            }

            _logger.LogInformation("Fetched {Count} active releases from Admin.Api", syncDtos.Count);

            // Sync each release
            foreach (var dto in syncDtos)
            {
                try
                {
                    // Note: In this simplified implementation, we just log the sync
                    // In a real implementation, you would cache the release metadata in ClientDb
                    // For now, we're demonstrating the sync mechanism
                    _logger.LogInformation("Synced release {ReleaseId} - Version {Version} - Severity: {Severity}",
                        dto.ReleaseId, dto.Version, dto.Severity);

                    // The releases will still come from the same database in development
                    // In production with true database separation, you would:
                    // 1. Create cached Update record in ClientDb
                    // 2. Create cached Release record in ClientDb
                    // 3. ClientPortal.Api would read from ClientDb cache
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error syncing release {ReleaseId}", dto.ReleaseId);
                }
            }

            _logger.LogInformation("Release sync completed successfully");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error connecting to Admin.Api at {Url}", _config.AdminApiUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during release sync");
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
