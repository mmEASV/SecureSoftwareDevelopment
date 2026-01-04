using Admin.Shared.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Admin.Api.Services;

/// <summary>
/// Service for sending webhook notifications to clients when new releases are created
/// </summary>
public interface IWebhookNotificationService
{
    Task NotifyClientsOfNewReleaseAsync(Release release, CancellationToken cancellationToken = default);
    Task<bool> TestWebhookAsync(Client client, CancellationToken cancellationToken = default);
}

public class WebhookNotificationService : IWebhookNotificationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookNotificationService> _logger;
    private readonly IConfiguration _configuration;

    public WebhookNotificationService(
        IHttpClientFactory httpClientFactory,
        ILogger<WebhookNotificationService> logger,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task NotifyClientsOfNewReleaseAsync(Release release, CancellationToken cancellationToken = default)
    {
        // In a real implementation, fetch clients from database
        // For now, get from configuration or use sample data
        var clients = GetRegisteredClients();

        _logger.LogInformation("Notifying {Count} clients of new release {ReleaseId}", clients.Count, release.Id);

        var tasks = clients
            .Where(c => c.IsActive)
            .Select(client => SendWebhookToClientAsync(client, release, cancellationToken));

        await Task.WhenAll(tasks);
    }

    public async Task<bool> TestWebhookAsync(Client client, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                EventType = "webhook.test",
                Timestamp = DateTime.UtcNow,
                ClientId = client.Id,
                Message = "Webhook test from Admin.Api"
            };

            var result = await SendWebhookAsync(client, payload, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook test failed for client {ClientId}", client.Id);
            return false;
        }
    }

    private async Task SendWebhookToClientAsync(Client client, Release release, CancellationToken cancellationToken)
    {
        try
        {
            var payload = new
            {
                EventType = "release.created",
                Timestamp = DateTime.UtcNow,
                ReleaseId = release.Id,
                UpdateId = release.UpdateId,
                ReleaseDate = release.ReleaseDate,
                IsMandatory = release.IsMandatory,
                MaxPostponeDays = release.MaxPostponeDays,
                IsActive = release.IsActive,
                Message = "New release available - please sync from /api/sync/releases/active"
            };

            var success = await SendWebhookAsync(client, payload, cancellationToken);

            if (success)
            {
                _logger.LogInformation("Webhook delivered successfully to client {ClientId} ({ClientName})",
                    client.Id, client.Name);
            }
            else
            {
                _logger.LogWarning("Webhook delivery failed to client {ClientId} ({ClientName})",
                    client.Id, client.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending webhook to client {ClientId} ({ClientName})",
                client.Id, client.Name);
        }
    }

    private async Task<bool> SendWebhookAsync(Client client, object payload, CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("WebhookClient");

            // Serialize payload
            var jsonPayload = JsonSerializer.Serialize(payload);

            // Generate HMAC signature for security
            var signature = GenerateHmacSignature(jsonPayload, client.WebhookSecret);

            // Create HTTP request
            var request = new HttpRequestMessage(HttpMethod.Post, client.WebhookUrl)
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };

            // Add signature header
            request.Headers.Add("X-Webhook-Signature", signature);
            request.Headers.Add("X-Webhook-Client-Id", client.Id.ToString());

            // Send request
            var response = await httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Webhook to {Url} returned {StatusCode}", client.WebhookUrl, response.StatusCode);
                return true;
            }
            else
            {
                _logger.LogWarning("Webhook to {Url} failed with status {StatusCode}",
                    client.WebhookUrl, response.StatusCode);
                return false;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error sending webhook to {Url}", client.WebhookUrl);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending webhook to {Url}", client.WebhookUrl);
            return false;
        }
    }

    private string GenerateHmacSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Get registered clients from configuration
    /// In production, this would query from database via IClientRepository
    /// </summary>
    private List<Client> GetRegisteredClients()
    {
        var webhookConfig = _configuration.GetSection("Webhooks:Clients").Get<List<WebhookClient>>();
        if (webhookConfig == null || !webhookConfig.Any())
        {
            _logger.LogWarning("No webhook clients configured in appsettings");
            return new List<Client>();
        }

        return webhookConfig.Select(wc => new Client
        {
            Id = Guid.NewGuid(),
            Name = wc.Name,
            WebhookUrl = wc.WebhookUrl,
            WebhookSecret = wc.WebhookSecret,
            IsActive = wc.IsActive
        }).ToList();
    }
}

/// <summary>
/// Configuration for webhook clients
/// </summary>
public class WebhookConfiguration
{
    public List<WebhookClient> Clients { get; set; } = new();
}

public class WebhookClient
{
    public string Name { get; set; } = string.Empty;
    public string WebhookUrl { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
