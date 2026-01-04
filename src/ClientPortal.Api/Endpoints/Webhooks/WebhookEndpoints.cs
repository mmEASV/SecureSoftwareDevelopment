using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ClientPortal.Api.Endpoints.Webhooks;

/// <summary>
/// Webhook endpoints for receiving notifications from Admin.Api
/// </summary>
public static class WebhookEndpoints
{
    public static void MapWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/webhooks")
            .WithTags("Webhooks");

        // Endpoint to receive release notifications from Admin.Api
        group.MapPost("/release-notification", HandleReleaseNotification)
            .WithName("ReceiveReleaseNotification")
            .WithSummary("Receive webhook notification when new release is created")
            .Produces(200)
            .Produces(401)
            .Produces(400);
    }

    private static async Task<IResult> HandleReleaseNotification(
        [FromBody] ReleaseNotificationPayload payload,
        [FromHeader(Name = "X-Webhook-Signature")] string signature,
        [FromHeader(Name = "X-Webhook-Client-Id")] string clientId,
        [FromServices] IConfiguration configuration,
        [FromServices] ILogger<Program> logger)
    {
        try
        {
            // Get webhook secret from configuration
            var webhookSecret = configuration["Webhooks:Secret"];
            if (string.IsNullOrEmpty(webhookSecret))
            {
                logger.LogWarning("Webhook secret not configured");
                return Results.Problem("Webhook not configured", statusCode: 500);
            }

            // Verify signature
            var jsonPayload = JsonSerializer.Serialize(payload);
            var expectedSignature = GenerateHmacSignature(jsonPayload, webhookSecret);

            if (signature != expectedSignature)
            {
                logger.LogWarning("Invalid webhook signature from client {ClientId}", clientId);
                return Results.Unauthorized();
            }

            // Process the notification
            logger.LogInformation("Received webhook notification: {EventType} for release {ReleaseId}",
                payload.EventType, payload.ReleaseId);

            // Trigger immediate sync instead of waiting for scheduled poll
            // In a real implementation, you would:
            // 1. Signal the ReleaseSyncService to sync immediately
            // 2. Or directly call the sync logic here
            // For now, just log it

            logger.LogInformation("New release {ReleaseId} available. Sync will occur on next scheduled interval or implement immediate sync here.",
                payload.ReleaseId);

            return Results.Ok(new
            {
                Status = "received",
                EventType = payload.EventType,
                ReleaseId = payload.ReleaseId,
                Timestamp = DateTime.UtcNow,
                Message = "Webhook notification processed successfully"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing webhook notification");
            return Results.Problem("Error processing webhook", statusCode: 500);
        }
    }

    private static string GenerateHmacSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash);
    }
}

/// <summary>
/// Payload received from Admin.Api webhook
/// </summary>
public record ReleaseNotificationPayload
{
    public string EventType { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public Guid ReleaseId { get; init; }
    public Guid UpdateId { get; init; }
    public DateTime ReleaseDate { get; init; }
    public bool IsMandatory { get; init; }
    public int MaxPostponeDays { get; init; }
    public bool IsActive { get; init; }
    public string Message { get; init; } = string.Empty;
}
