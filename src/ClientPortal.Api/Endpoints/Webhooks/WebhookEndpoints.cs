using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ClientPortal.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClientPortal.Api.Endpoints.Webhooks;

/// <summary>
/// Webhook endpoints for receiving notifications from Admin.Api
/// </summary>
public static class WebhookEndpoints
{
    public static void MapWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/webhooks")
            .WithTags("Webhooks");

        // Endpoint to receive release notifications from Admin.Api
        group.MapPost("/release-notification", HandleReleaseNotification)
            .WithName("ReceiveReleaseNotification")
            .WithSummary("Receive webhook notification when new release is created")
            .Produces(200)
            .Produces(401)
            .Produces(400);

        // Health check endpoint for webhook connectivity
        group.MapGet("/ping", () => Results.Ok(new
        {
            Status = "ok",
            Service = "ClientPortal.Api",
            Timestamp = DateTime.UtcNow
        }))
        .WithName("WebhookPing")
        .WithSummary("Health check endpoint for webhook connectivity");
    }

    private static async Task<IResult> HandleReleaseNotification(
        HttpContext context,
        [FromHeader(Name = "X-Webhook-Signature")] string signature,
        [FromHeader(Name = "X-Webhook-Client-Id")] string clientId,
        [FromServices] IConfiguration configuration,
        [FromServices] ISyncTriggerService syncTrigger,
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

            // Read raw body for signature verification
            // This is critical - we must verify the signature against the exact bytes received,
            // not against a re-serialized version which may have different formatting
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var rawBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            // Verify signature against RAW body
            var expectedSignature = GenerateHmacSignature(rawBody, webhookSecret);

            if (signature != expectedSignature)
            {
                logger.LogWarning("Invalid webhook signature from client {ClientId}", clientId);
                return Results.Unauthorized();
            }

            // Deserialize after signature verification
            var payload = JsonSerializer.Deserialize<ReleaseNotificationPayload>(rawBody);
            if (payload == null)
            {
                logger.LogWarning("Failed to deserialize webhook payload from client {ClientId}", clientId);
                return Results.BadRequest(new { Message = "Invalid payload" });
            }

            // Process the notification
            logger.LogInformation("Received webhook notification: {EventType} for release {ReleaseId}",
                payload.EventType, payload.ReleaseId);

            // Trigger immediate sync instead of waiting for scheduled poll
            var syncTriggered = syncTrigger.TriggerSync(payload.ReleaseId);

            if (syncTriggered)
            {
                logger.LogInformation("Immediate sync triggered for release {ReleaseId}", payload.ReleaseId);
            }
            else
            {
                logger.LogWarning("Failed to trigger immediate sync for release {ReleaseId} - sync already queued", payload.ReleaseId);
            }

            return Results.Ok(new
            {
                Status = "received",
                EventType = payload.EventType,
                ReleaseId = payload.ReleaseId,
                Timestamp = DateTime.UtcNow,
                SyncTriggered = syncTriggered,
                Message = syncTriggered
                    ? "Webhook notification processed and immediate sync triggered"
                    : "Webhook notification processed, sync already queued"
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
