using Admin.Api.Domain.Interfaces;
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
    Task<bool> NotifyClientOfReleaseAsync(Client client, Release release, CancellationToken cancellationToken = default);
    Task<bool> TestWebhookAsync(Client client, CancellationToken cancellationToken = default);
}

public class WebhookNotificationService : IWebhookNotificationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookNotificationService> _logger;
    private readonly IClientRepository _clientRepository;

    public WebhookNotificationService(
        IHttpClientFactory httpClientFactory,
        ILogger<WebhookNotificationService> logger,
        IClientRepository clientRepository)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _clientRepository = clientRepository;
    }

    public async Task NotifyClientsOfNewReleaseAsync(Release release, CancellationToken cancellationToken = default)
    {
        var clients = await _clientRepository.GetActiveClientsAsync(cancellationToken);

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

    public async Task<bool> NotifyClientOfReleaseAsync(Client client, Release release, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Manually notifying client {ClientId} ({ClientName}) of release {ReleaseId}",
                client.Id, client.Name, release.Id);

            return await SendWebhookToClientAsync(client, release, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manual webhook notification failed for client {ClientId}", client.Id);
            return false;
        }
    }

    private async Task<bool> SendWebhookToClientAsync(Client client, Release release, CancellationToken cancellationToken)
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

            // Update webhook status in database
            await _clientRepository.UpdateWebhookStatusAsync(client.Id, success, cancellationToken);

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

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending webhook to client {ClientId} ({ClientName})",
                client.Id, client.Name);
            // Update webhook status as failed
            await _clientRepository.UpdateWebhookStatusAsync(client.Id, false, CancellationToken.None);
            return false;
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
}
