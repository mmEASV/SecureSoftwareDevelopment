using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Admin.Api.Domain.Interfaces;
using Admin.Api.Services;
using Admin.Shared.Dto;
using Admin.Shared.Models;

namespace Admin.Api.Endpoints.Clients;

public static class ClientEndpoints
{
    public static RouteGroupBuilder MapClientEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetAllClients)
            .WithName("GetAllClients");

        group.MapGet("/{id:guid}", GetClientById)
            .WithName("GetClientById");

        group.MapPost("/", CreateClient)
            .WithName("CreateClient");

        group.MapPut("/{id:guid}", UpdateClient)
            .WithName("UpdateClient");

        group.MapDelete("/{id:guid}", DeleteClient)
            .WithName("DeleteClient");

        group.MapPost("/{id:guid}/test", TestWebhook)
            .WithName("TestWebhook");

        group.MapGet("/{id:guid}/health", CheckClientHealth)
            .WithName("CheckClientHealth");

        return group;
    }

    private static async Task<IResult> GetAllClients(
        [FromServices] IClientRepository clientRepository,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var clients = await clientRepository.GetAllAsync(includeInactive, cancellationToken);
        var dtos = clients.Select(MapToDto).ToList();
        return Results.Ok(dtos);
    }

    private static async Task<IResult> GetClientById(
        [FromRoute] Guid id,
        [FromServices] IClientRepository clientRepository,
        CancellationToken cancellationToken)
    {
        var client = await clientRepository.GetByIdAsync(id, cancellationToken);

        if (client == null)
        {
            return Results.NotFound(new { Message = $"Client with ID {id} not found" });
        }

        return Results.Ok(MapToDto(client));
    }

    private static async Task<IResult> CreateClient(
        [FromBody] CreateClientDto dto,
        [FromServices] IClientRepository clientRepository,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        // Generate a secure webhook secret
        var webhookSecret = GenerateWebhookSecret();

        var client = new Client
        {
            Name = dto.Name,
            Description = dto.Description,
            WebhookUrl = dto.WebhookUrl,
            WebhookSecret = webhookSecret,
            ContactEmail = dto.ContactEmail,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty // TODO: Get from authenticated user
        };

        var created = await clientRepository.CreateAsync(client, cancellationToken);

        logger.LogInformation("Created client {ClientId} ({ClientName})", created.Id, created.Name);

        return Results.Created($"/api/clients/{created.Id}", MapToDto(created));
    }

    private static async Task<IResult> UpdateClient(
        [FromRoute] Guid id,
        [FromBody] UpdateClientDto dto,
        [FromServices] IClientRepository clientRepository,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var client = await clientRepository.GetByIdAsync(id, cancellationToken);

        if (client == null)
        {
            return Results.NotFound(new { Message = $"Client with ID {id} not found" });
        }

        if (dto.Name != null) client.Name = dto.Name;
        if (dto.Description != null) client.Description = dto.Description;
        if (dto.WebhookUrl != null) client.WebhookUrl = dto.WebhookUrl;
        if (dto.IsActive.HasValue) client.IsActive = dto.IsActive.Value;
        if (dto.ContactEmail != null) client.ContactEmail = dto.ContactEmail;

        client.UpdatedAt = DateTime.UtcNow;
        client.UpdatedBy = Guid.Empty; // TODO: Get from authenticated user

        await clientRepository.UpdateAsync(client, cancellationToken);

        logger.LogInformation("Updated client {ClientId}", id);

        return Results.Ok(MapToDto(client));
    }

    private static async Task<IResult> DeleteClient(
        [FromRoute] Guid id,
        [FromServices] IClientRepository clientRepository,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var client = await clientRepository.GetByIdAsync(id, cancellationToken);

        if (client == null)
        {
            return Results.NotFound(new { Message = $"Client with ID {id} not found" });
        }

        await clientRepository.DeleteAsync(id, cancellationToken);

        logger.LogInformation("Deleted client {ClientId}", id);

        return Results.NoContent();
    }

    private static async Task<IResult> TestWebhook(
        [FromRoute] Guid id,
        [FromServices] IClientRepository clientRepository,
        [FromServices] IWebhookNotificationService webhookService,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var client = await clientRepository.GetByIdAsync(id, cancellationToken);

        if (client == null)
        {
            return Results.NotFound(new { Message = $"Client with ID {id} not found" });
        }

        logger.LogInformation("Testing webhook for client {ClientId} ({ClientName})", id, client.Name);

        var success = await webhookService.TestWebhookAsync(client, cancellationToken);

        // Update webhook status
        await clientRepository.UpdateWebhookStatusAsync(id, success, cancellationToken);

        if (success)
        {
            return Results.Ok(new { Message = "Webhook test successful", ClientId = id });
        }
        else
        {
            return Results.BadRequest(new { Message = "Webhook test failed", ClientId = id });
        }
    }

    private static async Task<IResult> CheckClientHealth(
        [FromRoute] Guid id,
        [FromServices] IClientRepository clientRepository,
        [FromServices] IClientHealthService healthService,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var client = await clientRepository.GetByIdAsync(id, cancellationToken);

        if (client == null)
        {
            return Results.NotFound(new { Message = $"Client with ID {id} not found" });
        }

        logger.LogInformation("Checking health for client {ClientId} ({ClientName})", id, client.Name);

        var result = await healthService.CheckHealthAsync(client, cancellationToken);

        // Update webhook status in database based on health check
        await clientRepository.UpdateWebhookStatusAsync(id, result.IsHealthy, cancellationToken);

        return Results.Ok(new
        {
            ClientId = id,
            ClientName = client.Name,
            IsHealthy = result.IsHealthy,
            ResponseTimeMs = result.ResponseTimeMs,
            LastChecked = DateTime.UtcNow,
            Error = result.Error
        });
    }

    private static string GenerateWebhookSecret()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static ClientDto MapToDto(Client client)
    {
        return new ClientDto
        {
            Id = client.Id,
            Name = client.Name,
            Description = client.Description,
            WebhookUrl = client.WebhookUrl,
            WebhookSecret = client.WebhookSecret,
            IsActive = client.IsActive,
            LastWebhookSuccess = client.LastWebhookSuccess,
            LastWebhookFailure = client.LastWebhookFailure,
            ConsecutiveFailures = client.ConsecutiveFailures,
            ContactEmail = client.ContactEmail,
            CreatedAt = client.CreatedAt
        };
    }
}
