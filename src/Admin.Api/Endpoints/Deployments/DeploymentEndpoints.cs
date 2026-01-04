using Microsoft.AspNetCore.Mvc;
using Admin.Api.Domain.Interfaces;
using Admin.Shared.Dto;
using Admin.Shared.Enums;
using Admin.Shared.Models;

namespace Admin.Api.Endpoints.Deployments;

public static class DeploymentEndpoints
{
    public static RouteGroupBuilder MapDeploymentEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetAllDeployments)
            .WithName("GetAllDeployments");

        group.MapGet("/{id:guid}", GetDeploymentById)
            .WithName("GetDeploymentById");

        group.MapPost("/schedule", ScheduleDeployment)
            .WithName("ScheduleDeployment");

        group.MapPut("/{id:guid}/postpone", PostponeDeployment)
            .WithName("PostponeDeployment");

        group.MapPut("/{id:guid}/status", UpdateDeploymentStatus)
            .WithName("UpdateDeploymentStatus");

        group.MapGet("/statistics", GetDeploymentStatistics)
            .WithName("GetDeploymentStatistics");

        group.MapDelete("/{id:guid}", CancelDeployment)
            .WithName("CancelDeployment");

        return group;
    }

    private static async Task<IResult> GetAllDeployments(
        [FromServices] IDeploymentRepository deploymentRepository,
        [FromQuery] Guid? deviceId = null,
        [FromQuery] Guid? releaseId = null,
        [FromQuery] DeploymentStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        List<Deployment> deployments;

        if (deviceId.HasValue)
        {
            deployments = await deploymentRepository.GetByDeviceIdAsync(deviceId.Value, cancellationToken);
        }
        else if (releaseId.HasValue)
        {
            deployments = await deploymentRepository.GetByReleaseIdAsync(releaseId.Value, cancellationToken);
        }
        else if (status.HasValue)
        {
            deployments = await deploymentRepository.GetByStatusAsync(status.Value, cancellationToken);
        }
        else
        {
            deployments = await deploymentRepository.GetAllAsync(cancellationToken);
        }

        var dtos = deployments.Select(MapToDto).ToList();
        return Results.Ok(dtos);
    }

    private static async Task<IResult> GetDeploymentById(
        [FromRoute] Guid id,
        [FromServices] IDeploymentRepository deploymentRepository,
        CancellationToken cancellationToken)
    {
        var deployment = await deploymentRepository.GetByIdAsync(id, cancellationToken);

        if (deployment == null)
        {
            return Results.NotFound(new { Message = $"Deployment with ID {id} not found" });
        }

        return Results.Ok(MapToDto(deployment));
    }

    private static async Task<IResult> ScheduleDeployment(
        [FromBody] ScheduleDeploymentDto dto,
        [FromServices] IDeploymentRepository deploymentRepository,
        [FromServices] IReleaseRepository releaseRepository,
        [FromServices] IDeviceRepository deviceRepository,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        // Validate release exists
        var release = await releaseRepository.GetByIdAsync(dto.ReleaseId, cancellationToken);
        if (release == null)
        {
            return Results.BadRequest(new { Message = $"Release with ID {dto.ReleaseId} not found" });
        }

        // Validate devices exist
        var deployments = new List<Deployment>();

        foreach (var deviceId in dto.DeviceIds)
        {
            var device = await deviceRepository.GetByIdAsync(deviceId, cancellationToken);
            if (device == null)
            {
                logger.LogWarning("Device {DeviceId} not found, skipping", deviceId);
                continue;
            }

            // Check if device already has a pending deployment for this release
            var existing = await deploymentRepository.GetPendingForDeviceAsync(deviceId, cancellationToken);
            if (existing.Any(d => d.ReleaseId == dto.ReleaseId))
            {
                logger.LogWarning("Device {DeviceId} already has a pending deployment for release {ReleaseId}, skipping",
                    deviceId, dto.ReleaseId);
                continue;
            }

            var deployment = new Deployment
            {
                ReleaseId = dto.ReleaseId,
                DeviceId = deviceId,
                ScheduledAt = dto.ScheduledAt ?? DateTime.UtcNow,
                Status = DeploymentStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty // TODO: Get from authenticated user
            };

            deployments.Add(deployment);
        }

        if (deployments.Count == 0)
        {
            return Results.BadRequest(new { Message = "No valid devices to schedule deployment for" });
        }

        var created = await deploymentRepository.CreateBulkAsync(deployments, cancellationToken);

        logger.LogInformation("Scheduled {Count} deployments for release {ReleaseId}", created.Count, dto.ReleaseId);

        return Results.Created("/api/deployments", new
        {
            Message = $"Scheduled {created.Count} deployments",
            DeploymentIds = created.Select(d => d.Id).ToList()
        });
    }

    private static async Task<IResult> PostponeDeployment(
        [FromRoute] Guid id,
        [FromBody] PostponeDeploymentDto dto,
        [FromServices] IDeploymentRepository deploymentRepository,
        [FromServices] IReleaseRepository releaseRepository,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var deployment = await deploymentRepository.GetByIdAsync(id, cancellationToken);

        if (deployment == null)
        {
            return Results.NotFound(new { Message = $"Deployment with ID {id} not found" });
        }

        // Get release to check postpone limits
        var release = await releaseRepository.GetByIdAsync(deployment.ReleaseId, cancellationToken);
        if (release == null)
        {
            return Results.BadRequest(new { Message = "Associated release not found" });
        }

        // Check if security update and if postpone is allowed
        if (release.IsMandatory)
        {
            var maxPostponeDate = deployment.CreatedAt.AddDays(release.MaxPostponeDays);
            if (dto.PostponeUntil > maxPostponeDate)
            {
                return Results.BadRequest(new
                {
                    Message = $"Mandatory updates can only be postponed up to {release.MaxPostponeDays} days (until {maxPostponeDate:yyyy-MM-dd})"
                });
            }
        }

        deployment.Status = DeploymentStatus.Postponed;
        deployment.ScheduledAt = dto.PostponeUntil;
        deployment.PostponeReason = dto.Reason;
        deployment.PostponeCount++;
        deployment.LastPostponedAt = DateTime.UtcNow;

        await deploymentRepository.UpdateAsync(deployment, cancellationToken);

        logger.LogInformation("Postponed deployment {Id} until {PostponeUntil}", id, dto.PostponeUntil);

        return Results.Ok(MapToDto(deployment));
    }

    private static async Task<IResult> UpdateDeploymentStatus(
        [FromRoute] Guid id,
        [FromBody] UpdateDeploymentStatusDto dto,
        [FromServices] IDeploymentRepository deploymentRepository,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var deployment = await deploymentRepository.GetByIdAsync(id, cancellationToken);

        if (deployment == null)
        {
            return Results.NotFound(new { Message = $"Deployment with ID {id} not found" });
        }

        var previousStatus = deployment.Status;
        deployment.Status = dto.Status;

        if (dto.Status == DeploymentStatus.Downloading && deployment.StartedAt == null)
        {
            deployment.StartedAt = DateTime.UtcNow;
        }

        if (dto.Status == DeploymentStatus.Completed || dto.Status == DeploymentStatus.Failed)
        {
            deployment.CompletedAt = DateTime.UtcNow;
        }

        if (dto.ErrorMessage != null) deployment.ErrorMessage = dto.ErrorMessage;
        if (dto.DownloadProgress.HasValue) deployment.DownloadProgress = dto.DownloadProgress.Value;
        if (dto.InstallProgress.HasValue) deployment.InstallProgress = dto.InstallProgress.Value;

        if (dto.Status == DeploymentStatus.Failed)
        {
            deployment.RetryCount++;
            deployment.LastRetryAt = DateTime.UtcNow;
        }

        await deploymentRepository.UpdateAsync(deployment, cancellationToken);

        logger.LogInformation("Updated deployment {Id} status from {PreviousStatus} to {NewStatus}",
            id, previousStatus, dto.Status);

        return Results.Ok(MapToDto(deployment));
    }

    private static async Task<IResult> GetDeploymentStatistics(
        [FromServices] IDeploymentRepository deploymentRepository,
        CancellationToken cancellationToken)
    {
        var allDeployments = await deploymentRepository.GetAllAsync(cancellationToken);

        var statistics = new DeploymentStatistics
        {
            Total = allDeployments.Count,
            PendingDeployments = allDeployments.Count(d => d.Status == DeploymentStatus.Pending),
            Downloading = allDeployments.Count(d => d.Status == DeploymentStatus.Downloading),
            Installing = allDeployments.Count(d => d.Status == DeploymentStatus.Installing),
            CompletedDeployments = allDeployments.Count(d => d.Status == DeploymentStatus.Completed),
            FailedDeployments = allDeployments.Count(d => d.Status == DeploymentStatus.Failed),
            Postponed = allDeployments.Count(d => d.Status == DeploymentStatus.Postponed),
            Cancelled = allDeployments.Count(d => d.Status == DeploymentStatus.Cancelled),
            SuccessRate = allDeployments.Count > 0
                ? (double)allDeployments.Count(d => d.Status == DeploymentStatus.Completed) / allDeployments.Count * 100
                : 0
        };

        return Results.Ok(statistics);
    }

    private static async Task<IResult> CancelDeployment(
        [FromRoute] Guid id,
        [FromServices] IDeploymentRepository deploymentRepository,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var deployment = await deploymentRepository.GetByIdAsync(id, cancellationToken);

        if (deployment == null)
        {
            return Results.NotFound(new { Message = $"Deployment with ID {id} not found" });
        }

        if (deployment.Status == DeploymentStatus.Completed)
        {
            return Results.BadRequest(new { Message = "Cannot cancel a completed deployment" });
        }

        deployment.Status = DeploymentStatus.Cancelled;
        await deploymentRepository.UpdateAsync(deployment, cancellationToken);

        logger.LogInformation("Cancelled deployment {Id}", id);

        return Results.Ok(MapToDto(deployment));
    }

    private static DeploymentDto MapToDto(Deployment deployment)
    {
        return new DeploymentDto
        {
            Id = deployment.Id,
            ReleaseId = deployment.ReleaseId,
            Release = deployment.Release != null ? MapReleaseToDto(deployment.Release) : null,
            DeviceId = deployment.DeviceId,
            Device = deployment.Device != null ? MapDeviceToDto(deployment.Device) : null,
            ScheduledAt = deployment.ScheduledAt,
            StartedAt = deployment.StartedAt,
            CompletedAt = deployment.CompletedAt,
            Status = deployment.Status,
            ErrorMessage = deployment.ErrorMessage,
            RetryCount = deployment.RetryCount,
            PostponeReason = deployment.PostponeReason,
            PostponeCount = deployment.PostponeCount,
            LastPostponedAt = deployment.LastPostponedAt,
            DownloadProgress = deployment.DownloadProgress,
            InstallProgress = deployment.InstallProgress,
            CreatedAt = deployment.CreatedAt
        };
    }

    private static ReleaseDto MapReleaseToDto(Release release)
    {
        return new ReleaseDto
        {
            Id = release.Id,
            UpdateId = release.UpdateId,
            Update = release.Update != null ? MapUpdateToDto(release.Update) : null,
            ReleaseDate = release.ReleaseDate,
            IsActive = release.IsActive,
            IsMandatory = release.IsMandatory,
            MinimumVersion = release.MinimumVersion,
            MaxPostponeDays = release.MaxPostponeDays,
            ReleaseNotes = release.ReleaseNotes,
            CreatedAt = release.CreatedAt
        };
    }

    private static UpdateDto MapUpdateToDto(Update update)
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
            FileSize = update.FileSize,
            UpdateType = update.UpdateType,
            Severity = update.Severity,
            IsSecurityUpdate = update.IsSecurityUpdate,
            TargetDeviceTypes = update.TargetDeviceTypes,
            CreatedAt = update.CreatedAt,
            IsActive = update.IsActive
        };
    }

    private static DeviceDto MapDeviceToDto(Device device)
    {
        return new DeviceDto
        {
            Id = device.Id,
            DeviceIdentifier = device.DeviceIdentifier,
            DeviceName = device.DeviceName,
            DeviceType = device.DeviceType,
            TenantId = device.TenantId,
            CurrentVersion = device.CurrentVersion,
            LastSeenAt = device.LastSeenAt,
            LastUpdateCheck = device.LastUpdateCheck,
            IsActive = device.IsActive,
            RegisteredAt = device.RegisteredAt,
            AutomaticUpdates = device.AutomaticUpdates,
            UpdateSchedule = device.UpdateSchedule,
            SkipNonSecurityUpdates = device.SkipNonSecurityUpdates,
            PostponeSecurityUpdates = device.PostponeSecurityUpdates,
            MaintenanceWindowStart = device.MaintenanceWindowStart,
            MaintenanceWindowEnd = device.MaintenanceWindowEnd
        };
    }
}
