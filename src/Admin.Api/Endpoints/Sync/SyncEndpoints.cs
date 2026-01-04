using Admin.Api.Domain.Interfaces;
using Admin.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace Admin.Api.Endpoints.Sync;

/// <summary>
/// Sync endpoints for client to pull updates from server through Cloudflare tunnel
/// </summary>
public static class SyncEndpoints
{
    public static void MapSyncEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sync")
            .WithTags("Sync")
            .WithOpenApi();

        // Endpoint for clients to pull active releases with their updates
        group.MapGet("/releases/active", GetActiveReleasesForSync)
            .WithName("SyncActiveReleases")
            .WithSummary("Get all active releases with update metadata for client synchronization")
            .Produces<List<ReleaseSyncDto>>(200);

        // Endpoint for clients to pull update file metadata
        group.MapGet("/updates/{id:guid}/metadata", GetUpdateMetadata)
            .WithName("SyncUpdateMetadata")
            .WithSummary("Get update file metadata including hash and signature for verification")
            .Produces<UpdateMetadataDto>(200)
            .Produces(404);
    }

    private static async Task<IResult> GetActiveReleasesForSync(
        [FromServices] IReleaseRepository releaseRepository,
        [FromServices] IUpdateRepository updateRepository)
    {
        var releases = await releaseRepository.GetActiveReleasesAsync();

        var syncDtos = new List<ReleaseSyncDto>();

        foreach (var release in releases)
        {
            var update = await updateRepository.GetByIdAsync(release.UpdateId);
            if (update == null) continue;

            var fileInfo = new FileInfo(update.FilePath);

            syncDtos.Add(new ReleaseSyncDto
            {
                ReleaseId = release.Id,
                UpdateId = update.Id,
                Version = update.Version,
                ReleaseDate = release.ReleaseDate,
                IsMandatory = release.IsMandatory,
                MaxPostponeDays = release.MaxPostponeDays,
                Severity = update.Severity.ToString(),
                Changelog = update.ChangeLog ?? string.Empty,
                CVEList = string.Join(", ", update.SecurityFixes),
                FileHash = update.FileHash,
                Signature = update.DigitalSignature,
                FileName = Path.GetFileName(update.FilePath),
                FileSizeBytes = fileInfo.Exists ? fileInfo.Length : 0
            });
        }

        return Results.Ok(syncDtos);
    }

    private static async Task<IResult> GetUpdateMetadata(
        Guid id,
        [FromServices] IUpdateRepository updateRepository)
    {
        var update = await updateRepository.GetByIdAsync(id);
        if (update == null)
        {
            return Results.NotFound($"Update with ID {id} not found");
        }

        var fileInfo = new FileInfo(update.FilePath);

        var metadata = new UpdateMetadataDto
        {
            Id = update.Id,
            Version = update.Version,
            FileName = Path.GetFileName(update.FilePath),
            FileHash = update.FileHash,
            Signature = update.DigitalSignature,
            FileSizeBytes = fileInfo.Exists ? fileInfo.Length : 0,
            Changelog = update.ChangeLog ?? string.Empty,
            CVEList = string.Join(", ", update.SecurityFixes)
        };

        return Results.Ok(metadata);
    }
}

/// <summary>
/// DTO for syncing releases to client database
/// </summary>
public record ReleaseSyncDto
{
    public Guid ReleaseId { get; init; }
    public Guid UpdateId { get; init; }
    public string Version { get; init; } = string.Empty;
    public DateTime ReleaseDate { get; init; }
    public bool IsMandatory { get; init; }
    public int MaxPostponeDays { get; init; }
    public string Severity { get; init; } = string.Empty;
    public string Changelog { get; init; } = string.Empty;
    public string CVEList { get; init; } = string.Empty;
    public string FileHash { get; init; } = string.Empty;
    public string Signature { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
}

/// <summary>
/// DTO for update file metadata
/// </summary>
public record UpdateMetadataDto
{
    public Guid Id { get; init; }
    public string Version { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string FileHash { get; init; } = string.Empty;
    public string Signature { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public string Changelog { get; init; } = string.Empty;
    public string CVEList { get; init; } = string.Empty;
}
