using Microsoft.AspNetCore.Mvc;
using Admin.Api.Domain.Interfaces;
using Admin.Shared.Dto;
using Admin.Shared.Models;

namespace Admin.Api.Endpoints.Devices;

public static class DeviceEndpoints
{
    public static RouteGroupBuilder MapDeviceEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetAllDevices)
            .WithName("GetAllDevices");

        group.MapGet("/{id:guid}", GetDeviceById)
            .WithName("GetDeviceById");

        group.MapPost("/", RegisterDevice)
            .WithName("RegisterDevice");

        group.MapPut("/{id:guid}", UpdateDevice)
            .WithName("UpdateDevice");

        group.MapPut("/{id:guid}/settings", UpdateDeviceSettings)
            .WithName("UpdateDeviceSettings");

        group.MapGet("/{id:guid}/deployments", GetDeviceDeployments)
            .WithName("GetDeviceDeployments");

        group.MapDelete("/{id:guid}", DeleteDevice)
            .WithName("DeleteDevice");

        return group;
    }

    private static async Task<IResult> GetAllDevices(
        [FromServices] IDeviceRepository deviceRepository,
        [FromQuery] Guid? tenantId = null,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        List<Device> devices;

        if (tenantId.HasValue)
        {
            devices = await deviceRepository.GetByTenantIdAsync(tenantId.Value, includeInactive, cancellationToken);
        }
        else
        {
            devices = await deviceRepository.GetAllAsync(includeInactive, cancellationToken);
        }

        var dtos = devices.Select(MapToDto).ToList();
        return Results.Ok(dtos);
    }

    private static async Task<IResult> GetDeviceById(
        [FromRoute] Guid id,
        [FromServices] IDeviceRepository deviceRepository,
        CancellationToken cancellationToken)
    {
        var device = await deviceRepository.GetByIdAsync(id, cancellationToken);

        if (device == null)
        {
            return Results.NotFound(new { Message = $"Device with ID {id} not found" });
        }

        return Results.Ok(MapToDto(device));
    }

    private static async Task<IResult> RegisterDevice(
        [FromBody] RegisterDeviceDto dto,
        [FromServices] IDeviceRepository deviceRepository,
        [FromServices] ILogger<Program> logger,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Check if device already exists
        var existing = await deviceRepository.GetByIdentifierAsync(dto.DeviceIdentifier, cancellationToken);
        if (existing != null)
        {
            return Results.Conflict(new { Message = $"Device with identifier {dto.DeviceIdentifier} already exists" });
        }

        // TODO: Extract tenant ID from authenticated user
        var tenantId = Guid.NewGuid(); // Temporary

        var device = new Device
        {
            DeviceIdentifier = dto.DeviceIdentifier,
            DeviceName = dto.DeviceName,
            DeviceType = dto.DeviceType,
            TenantId = tenantId,
            CurrentVersion = dto.CurrentVersion,
            RegisteredAt = DateTime.UtcNow,
            AutomaticUpdates = true, // CRA: enabled by default
            ApiKey = GenerateApiKey()
        };

        var created = await deviceRepository.CreateAsync(device, cancellationToken);

        logger.LogInformation("Registered device {DeviceId} for tenant {TenantId}", created.DeviceIdentifier, created.TenantId);

        return Results.Created($"/api/devices/{created.Id}", MapToDto(created));
    }

    private static async Task<IResult> UpdateDevice(
        [FromRoute] Guid id,
        [FromBody] UpdateDeviceDto dto,
        [FromServices] IDeviceRepository deviceRepository,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var device = await deviceRepository.GetByIdAsync(id, cancellationToken);

        if (device == null)
        {
            return Results.NotFound(new { Message = $"Device with ID {id} not found" });
        }

        if (dto.DeviceName != null) device.DeviceName = dto.DeviceName;
        if (dto.CurrentVersion != null) device.CurrentVersion = dto.CurrentVersion;
        if (dto.IsActive.HasValue) device.IsActive = dto.IsActive.Value;

        device.LastSeenAt = DateTime.UtcNow;

        await deviceRepository.UpdateAsync(device, cancellationToken);

        logger.LogInformation("Updated device {Id}", id);

        return Results.Ok(MapToDto(device));
    }

    private static async Task<IResult> UpdateDeviceSettings(
        [FromRoute] Guid id,
        [FromBody] UpdateDeviceSettingsDto dto,
        [FromServices] IDeviceRepository deviceRepository,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var device = await deviceRepository.GetByIdAsync(id, cancellationToken);

        if (device == null)
        {
            return Results.NotFound(new { Message = $"Device with ID {id} not found" });
        }

        // CRA compliance: Allow opt-out of automatic updates
        if (dto.AutomaticUpdates.HasValue) device.AutomaticUpdates = dto.AutomaticUpdates.Value;
        if (dto.UpdateSchedule != null) device.UpdateSchedule = dto.UpdateSchedule;
        if (dto.SkipNonSecurityUpdates.HasValue) device.SkipNonSecurityUpdates = dto.SkipNonSecurityUpdates.Value;
        if (dto.PostponeSecurityUpdates.HasValue) device.PostponeSecurityUpdates = dto.PostponeSecurityUpdates.Value;
        if (dto.MaintenanceWindowStart != null) device.MaintenanceWindowStart = dto.MaintenanceWindowStart;
        if (dto.MaintenanceWindowEnd != null) device.MaintenanceWindowEnd = dto.MaintenanceWindowEnd;

        await deviceRepository.UpdateAsync(device, cancellationToken);

        logger.LogInformation("Updated settings for device {Id}", id);

        return Results.Ok(MapToDto(device));
    }

    private static async Task<IResult> GetDeviceDeployments(
        [FromRoute] Guid id,
        [FromServices] IDeploymentRepository deploymentRepository,
        CancellationToken cancellationToken)
    {
        var deployments = await deploymentRepository.GetByDeviceIdAsync(id, cancellationToken);
        var dtos = deployments.Select(MapDeploymentToDto).ToList();
        return Results.Ok(dtos);
    }

    private static async Task<IResult> DeleteDevice(
        [FromRoute] Guid id,
        [FromServices] IDeviceRepository deviceRepository,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        await deviceRepository.DeleteAsync(id, cancellationToken);

        logger.LogInformation("Deleted device {Id}", id);

        return Results.NoContent();
    }

    private static string GenerateApiKey()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }

    private static DeviceDto MapToDto(Device device)
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

    private static DeploymentDto MapDeploymentToDto(Deployment deployment)
    {
        return new DeploymentDto
        {
            Id = deployment.Id,
            ReleaseId = deployment.ReleaseId,
            DeviceId = deployment.DeviceId,
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
}
