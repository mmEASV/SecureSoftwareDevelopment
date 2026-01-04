using Microsoft.AspNetCore.Mvc;
using ClientPortal.Api.Domain.Interfaces;
using Admin.Shared.Dto;
using Admin.Shared.Models;
using System.Security.Cryptography;

namespace ClientPortal.Api.Endpoints.Devices;

public static class DeviceEndpoints
{
    public static RouteGroupBuilder MapDeviceEndpoints(this RouteGroupBuilder group)
    {
        // Customer-facing: Register and manage their own devices
        group.MapPost("/", RegisterDevice)
            .WithName("RegisterDevice")
            .WithOpenApi();

        group.MapGet("/{id:guid}", GetDeviceById)
            .WithName("GetDeviceById")
            .WithOpenApi();

        group.MapPut("/{id:guid}/settings", UpdateDeviceSettings)
            .WithName("UpdateDeviceSettings")
            .WithOpenApi();

        group.MapGet("/{id:guid}/deployments", GetDeviceDeployments)
            .WithName("GetDeviceDeployments")
            .WithOpenApi();

        return group;
    }

    private static async Task<IResult> RegisterDevice(
        [FromBody] RegisterDeviceDto dto,
        [FromServices] IDeviceRepository deviceRepository,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        // Check if device already exists
        var existing = await deviceRepository.GetByIdentifierAsync(dto.DeviceIdentifier, cancellationToken);
        if (existing != null)
        {
            return Results.Conflict(new { Message = $"Device with identifier {dto.DeviceIdentifier} already exists" });
        }

        // TODO: Extract tenant ID from authenticated user (for now, use a default)
        var tenantId = Guid.NewGuid();

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

        logger.LogInformation("Registered device {Identifier} with ID {Id}", created.DeviceIdentifier, created.Id);

        return Results.Created($"/api/devices/{created.Id}", new DeviceDto
        {
            Id = created.Id,
            DeviceIdentifier = created.DeviceIdentifier,
            DeviceName = created.DeviceName,
            DeviceType = created.DeviceType,
            CurrentVersion = created.CurrentVersion,
            RegisteredAt = created.RegisteredAt,
            AutomaticUpdates = created.AutomaticUpdates,
            TenantId = created.TenantId
        });
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

        return Results.Ok(new DeviceDto
        {
            Id = device.Id,
            DeviceIdentifier = device.DeviceIdentifier,
            DeviceName = device.DeviceName,
            DeviceType = device.DeviceType,
            CurrentVersion = device.CurrentVersion,
            RegisteredAt = device.RegisteredAt,
            LastSeenAt = device.LastSeenAt,
            AutomaticUpdates = device.AutomaticUpdates,
            SkipNonSecurityUpdates = device.SkipNonSecurityUpdates,
            TenantId = device.TenantId
        });
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

        // Update settings
        if (dto.AutomaticUpdates.HasValue)
            device.AutomaticUpdates = dto.AutomaticUpdates.Value;
        if (dto.SkipNonSecurityUpdates.HasValue)
            device.SkipNonSecurityUpdates = dto.SkipNonSecurityUpdates.Value;

        await deviceRepository.UpdateAsync(device, cancellationToken);

        logger.LogInformation("Updated device {Id} settings", id);

        return Results.Ok(new DeviceDto
        {
            Id = device.Id,
            DeviceIdentifier = device.DeviceIdentifier,
            DeviceName = device.DeviceName,
            DeviceType = device.DeviceType,
            CurrentVersion = device.CurrentVersion,
            RegisteredAt = device.RegisteredAt,
            LastSeenAt = device.LastSeenAt,
            AutomaticUpdates = device.AutomaticUpdates,
            SkipNonSecurityUpdates = device.SkipNonSecurityUpdates,
            TenantId = device.TenantId
        });
    }

    private static async Task<IResult> GetDeviceDeployments(
        [FromRoute] Guid id,
        [FromServices] IDeviceRepository deviceRepository,
        [FromServices] IDeploymentRepository deploymentRepository,
        CancellationToken cancellationToken)
    {
        var device = await deviceRepository.GetByIdAsync(id, cancellationToken);

        if (device == null)
        {
            return Results.NotFound(new { Message = $"Device with ID {id} not found" });
        }

        var deployments = await deploymentRepository.GetByDeviceIdAsync(id, cancellationToken);

        var dtos = deployments.Select(d => new DeploymentDto
        {
            Id = d.Id,
            ReleaseId = d.ReleaseId,
            DeviceId = d.DeviceId,
            Status = d.Status,
            ScheduledAt = d.ScheduledAt,
            StartedAt = d.StartedAt,
            CompletedAt = d.CompletedAt,
            ErrorMessage = d.ErrorMessage,
            PostponeReason = d.PostponeReason,
            PostponeCount = d.PostponeCount
        }).ToList();

        return Results.Ok(dtos);
    }

    private static string GenerateApiKey()
    {
        // Generate a 64-byte (512-bit) API key
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray()) +
               Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }
}
