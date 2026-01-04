using Microsoft.AspNetCore.Mvc;
using ClientPortal.Api.Domain.Interfaces;
using Admin.Shared.Dto;
using Admin.Shared.Models;
using Admin.Shared.Enums;

namespace ClientPortal.Api.Endpoints.Deployments;

public static class DeploymentEndpoints
{
    public static RouteGroupBuilder MapDeploymentEndpoints(this RouteGroupBuilder group)
    {
        // Customer-facing: Schedule and postpone deployments for their devices
        group.MapPost("/schedule", ScheduleDeployment)
            .WithName("ScheduleDeployment")
            .WithOpenApi();

        group.MapPut("/{id:guid}/postpone", PostponeDeployment)
            .WithName("PostponeDeployment")
            .WithOpenApi();

        group.MapGet("/{id:guid}", GetDeploymentById)
            .WithName("GetDeploymentById")
            .WithOpenApi();

        return group;
    }

    private static async Task<IResult> ScheduleDeployment(
        [FromBody] ScheduleDeploymentDto dto,
        [FromServices] IDeploymentRepository deploymentRepository,
        [FromServices] IReleaseRepository releaseRepository,
        [FromServices] IDeviceRepository deviceRepository,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        // Verify release exists
        var release = await releaseRepository.GetByIdAsync(dto.ReleaseId, cancellationToken);
        if (release == null)
        {
            return Results.NotFound(new { Message = $"Release with ID {dto.ReleaseId} not found" });
        }

        var deployments = new List<Deployment>();

        foreach (var deviceId in dto.DeviceIds)
        {
            // Verify device exists
            var device = await deviceRepository.GetByIdAsync(deviceId, cancellationToken);
            if (device == null)
            {
                logger.LogWarning("Device {DeviceId} not found, skipping", deviceId);
                continue;
            }

            // Check if deployment already exists for this device/release
            var existingDeployments = await deploymentRepository.GetByDeviceIdAsync(deviceId, cancellationToken);
            var existing = existingDeployments.FirstOrDefault(d => d.ReleaseId == dto.ReleaseId && d.Status != DeploymentStatus.Completed && d.Status != DeploymentStatus.Failed);

            if (existing != null)
            {
                logger.LogWarning("Deployment already exists for device {DeviceId} and release {ReleaseId}", deviceId, dto.ReleaseId);
                continue;
            }

            var deployment = new Deployment
            {
                ReleaseId = dto.ReleaseId,
                DeviceId = deviceId,
                Status = DeploymentStatus.Pending,
                ScheduledAt = dto.ScheduledAt ?? DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            var created = await deploymentRepository.CreateAsync(deployment, cancellationToken);
            deployments.Add(created);

            logger.LogInformation("Scheduled deployment {Id} for device {DeviceId} and release {ReleaseId}",
                created.Id, deviceId, dto.ReleaseId);
        }

        var dtos = deployments.Select(d => new DeploymentDto
        {
            Id = d.Id,
            ReleaseId = d.ReleaseId,
            DeviceId = d.DeviceId,
            Status = d.Status,
            ScheduledAt = d.ScheduledAt,
            StartedAt = d.StartedAt,
            CompletedAt = d.CompletedAt
        }).ToList();

        return Results.Ok(new { Message = $"Scheduled {deployments.Count} deployments", Deployments = dtos });
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

        // Can't postpone completed or failed deployments
        if (deployment.Status == DeploymentStatus.Completed || deployment.Status == DeploymentStatus.Failed)
        {
            return Results.BadRequest(new { Message = $"Cannot postpone deployment with status {deployment.Status}" });
        }

        // Check if release allows postponing
        var release = await releaseRepository.GetByIdAsync(deployment.ReleaseId, cancellationToken);
        if (release == null)
        {
            return Results.NotFound(new { Message = $"Release with ID {deployment.ReleaseId} not found" });
        }

        // Check max postpone period
        if (release.IsMandatory && release.MaxPostponeDays > 0)
        {
            var daysSinceRelease = (DateTime.UtcNow - release.ReleaseDate).Days;
            var daysUntilPostpone = (dto.PostponeUntil - DateTime.UtcNow).Days;

            if (daysSinceRelease + daysUntilPostpone > release.MaxPostponeDays)
            {
                return Results.BadRequest(new
                {
                    Message = $"Cannot postpone beyond maximum postpone period of {release.MaxPostponeDays} days",
                    MaxPostponeDays = release.MaxPostponeDays,
                    DaysSinceRelease = daysSinceRelease
                });
            }
        }

        deployment.Status = DeploymentStatus.Postponed;
        deployment.ScheduledAt = dto.PostponeUntil;
        deployment.PostponeReason = dto.Reason;
        deployment.PostponeCount++;

        await deploymentRepository.UpdateAsync(deployment, cancellationToken);

        logger.LogInformation("Postponed deployment {Id} until {PostponeUntil}. Reason: {Reason}",
            id, dto.PostponeUntil, dto.Reason);

        return Results.Ok(new DeploymentDto
        {
            Id = deployment.Id,
            ReleaseId = deployment.ReleaseId,
            DeviceId = deployment.DeviceId,
            Status = deployment.Status,
            ScheduledAt = deployment.ScheduledAt,
            PostponeReason = deployment.PostponeReason,
            PostponeCount = deployment.PostponeCount
        });
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

        return Results.Ok(new DeploymentDto
        {
            Id = deployment.Id,
            ReleaseId = deployment.ReleaseId,
            DeviceId = deployment.DeviceId,
            Status = deployment.Status,
            ScheduledAt = deployment.ScheduledAt,
            StartedAt = deployment.StartedAt,
            CompletedAt = deployment.CompletedAt,
            ErrorMessage = deployment.ErrorMessage,
            PostponeReason = deployment.PostponeReason,
            PostponeCount = deployment.PostponeCount,
            RetryCount = deployment.RetryCount
        });
    }
}
