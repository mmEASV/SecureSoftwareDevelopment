using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Admin.Api.Domain.Interfaces;
using Admin.Api.Infrastructure.Storage;
using Admin.Shared.Dto;
using Admin.Shared.Models;

namespace Admin.Api.Endpoints.Updates;

public static class UpdateEndpoints
{
    public static RouteGroupBuilder MapUpdateEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetAllUpdates)
            .WithName("GetAllUpdates");

        group.MapGet("/{id:guid}", GetUpdateById)
            .WithName("GetUpdateById");

        group.MapPost("/", CreateUpdate)
            .WithName("CreateUpdate")
            .DisableAntiforgery();

        group.MapPut("/{id:guid}", UpdateUpdate)
            .WithName("UpdateUpdate");

        group.MapDelete("/{id:guid}", DeleteUpdate)
            .WithName("DeleteUpdate");

        group.MapGet("/{id:guid}/download", DownloadUpdate)
            .WithName("DownloadUpdate");

        return group;
    }

    private static async Task<IResult> GetAllUpdates(
        [FromServices] IUpdateRepository updateRepository,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var updates = await updateRepository.GetAllAsync(includeInactive, cancellationToken);
        var dtos = updates.Select(MapToDto).ToList();
        return Results.Ok(dtos);
    }

    private static async Task<IResult> GetUpdateById(
        [FromRoute] Guid id,
        [FromServices] IUpdateRepository updateRepository,
        CancellationToken cancellationToken)
    {
        var update = await updateRepository.GetByIdAsync(id, cancellationToken);

        if (update == null)
        {
            return Results.NotFound(new { Message = $"Update with ID {id} not found" });
        }

        return Results.Ok(MapToDto(update));
    }

    private static async Task<IResult> CreateUpdate(
        HttpContext httpContext,
        [FromForm] IFormFile file,
        [FromServices] IUpdateRepository updateRepository,
        [FromServices] IFileStorageService fileStorage,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate file
            if (file == null || file.Length == 0)
            {
                return Results.BadRequest(new { Message = "File is required" });
            }

            // Manually read form data
            var form = await httpContext.Request.ReadFormAsync(cancellationToken);

            var dto = new CreateUpdateDto
            {
                Version = form["Version"].ToString(),
                Title = form["Title"].ToString(),
                Description = form["Description"].ToString(),
                ChangeLog = form["ChangeLog"].ToString(),
                UpdateType = Enum.Parse<Admin.Shared.Enums.UpdateType>(form["UpdateType"].ToString()),
                Severity = Enum.Parse<Admin.Shared.Enums.UpdateSeverity>(form["Severity"].ToString()),
                IsSecurityUpdate = bool.Parse(form["IsSecurityUpdate"].ToString())
            };

            // Deserialize JSON list parameters
            if (!string.IsNullOrWhiteSpace(form["SecurityFixes"]))
            {
                dto.SecurityFixes = JsonSerializer.Deserialize<List<string>>(form["SecurityFixes"].ToString()) ?? new List<string>();
            }

            if (!string.IsNullOrWhiteSpace(form["TargetDeviceTypes"]))
            {
                dto.TargetDeviceTypes = JsonSerializer.Deserialize<List<string>>(form["TargetDeviceTypes"].ToString()) ?? new List<string>();
            }

            // Check if version already exists
            var existingUpdate = await updateRepository.GetByVersionAsync(dto.Version, cancellationToken);
            if (existingUpdate != null)
            {
                return Results.Conflict(new { Message = $"Update version {dto.Version} already exists" });
            }

            // Save file to storage (computes hash and creates digital signature)
            using var stream = file.OpenReadStream();
            var (filePath, fileHash, digitalSignature, fileSize) = await fileStorage.SaveFileAsync(stream, file.FileName, cancellationToken);

            // Create update entity
            var update = new Update
            {
                Version = dto.Version,
                Title = dto.Title,
                Description = dto.Description,
                ChangeLog = dto.ChangeLog,
                SecurityFixes = dto.SecurityFixes,
                UpdateType = dto.UpdateType,
                Severity = dto.Severity,
                IsSecurityUpdate = dto.IsSecurityUpdate,
                TargetDeviceTypes = dto.TargetDeviceTypes,
                FilePath = filePath,
                FileHash = fileHash,
                DigitalSignature = digitalSignature,
                FileSize = fileSize,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty // TODO: Get from authenticated user
            };

            var created = await updateRepository.CreateAsync(update, cancellationToken);

            logger.LogInformation("Created update {Version} with ID {Id}", created.Version, created.Id);

            return Results.Created($"/api/updates/{created.Id}", MapToDto(created));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating update");
            return Results.Problem("An error occurred while creating the update");
        }
    }

    private static async Task<IResult> UpdateUpdate(
        [FromRoute] Guid id,
        [FromBody] UpdateUpdateDto dto,
        [FromServices] IUpdateRepository updateRepository,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var update = await updateRepository.GetByIdAsync(id, cancellationToken);

        if (update == null)
        {
            return Results.NotFound(new { Message = $"Update with ID {id} not found" });
        }

        // Update properties
        if (dto.Title != null) update.Title = dto.Title;
        if (dto.Description != null) update.Description = dto.Description;
        if (dto.ChangeLog != null) update.ChangeLog = dto.ChangeLog;
        if (dto.SecurityFixes != null) update.SecurityFixes = dto.SecurityFixes;
        if (dto.UpdateType.HasValue) update.UpdateType = dto.UpdateType.Value;
        if (dto.Severity.HasValue) update.Severity = dto.Severity.Value;
        if (dto.IsSecurityUpdate.HasValue) update.IsSecurityUpdate = dto.IsSecurityUpdate.Value;
        if (dto.IsActive.HasValue) update.IsActive = dto.IsActive.Value;

        update.UpdatedAt = DateTime.UtcNow;
        update.UpdatedBy = Guid.Empty; // TODO: Get from authenticated user

        await updateRepository.UpdateAsync(update, cancellationToken);

        logger.LogInformation("Updated update {Id}", id);

        return Results.Ok(MapToDto(update));
    }

    private static async Task<IResult> DeleteUpdate(
        [FromRoute] Guid id,
        [FromServices] IUpdateRepository updateRepository,
        [FromServices] IFileStorageService fileStorage,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var update = await updateRepository.GetByIdAsync(id, cancellationToken);

        if (update == null)
        {
            return Results.NotFound(new { Message = $"Update with ID {id} not found" });
        }

        // Delete file from storage
        await fileStorage.DeleteFileAsync(update.FilePath, cancellationToken);

        // Delete update from database
        await updateRepository.DeleteAsync(id, cancellationToken);

        logger.LogInformation("Deleted update {Id}", id);

        return Results.NoContent();
    }

    private static async Task<IResult> DownloadUpdate(
        [FromRoute] Guid id,
        [FromServices] IUpdateRepository updateRepository,
        [FromServices] IFileStorageService fileStorage,
        CancellationToken cancellationToken)
    {
        var update = await updateRepository.GetByIdAsync(id, cancellationToken);

        if (update == null)
        {
            return Results.NotFound(new { Message = $"Update with ID {id} not found" });
        }

        var fileStream = await fileStorage.GetFileAsync(update.FilePath, cancellationToken);

        if (fileStream == null)
        {
            return Results.NotFound(new { Message = "Update file not found" });
        }

        return Results.File(fileStream, "application/octet-stream", $"update-{update.Version}.bin");
    }

    private static UpdateDto MapToDto(Update update)
    {
        return new UpdateDto
        {
            Id = update.Id,
            Version = update.Version,
            Title = update.Title,
            Description = update.Description,
            ChangeLog = update.ChangeLog,
            SecurityFixes = update.SecurityFixes,
            FileHash = update.FileHash,
            DigitalSignature = update.DigitalSignature,
            FileSize = update.FileSize,
            UpdateType = update.UpdateType,
            Severity = update.Severity,
            IsSecurityUpdate = update.IsSecurityUpdate,
            TargetDeviceTypes = update.TargetDeviceTypes,
            CreatedAt = update.CreatedAt,
            IsActive = update.IsActive
        };
    }
}
