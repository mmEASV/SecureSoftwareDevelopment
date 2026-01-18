using System.Diagnostics;
using Admin.Shared.Models;

namespace Admin.Api.Services;

public interface IClientHealthService
{
    Task<ClientHealthResult> CheckHealthAsync(Client client, CancellationToken cancellationToken = default);
}

public record ClientHealthResult(bool IsHealthy, long ResponseTimeMs, string? Error);

public class ClientHealthService : IClientHealthService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ClientHealthService> _logger;

    public ClientHealthService(IHttpClientFactory httpClientFactory, ILogger<ClientHealthService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<ClientHealthResult> CheckHealthAsync(Client client, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var httpClient = _httpClientFactory.CreateClient("WebhookClient");

            // Derive health URL from webhook URL
            // WebhookUrl: http://localhost:5170/api/webhooks/release-notification
            // HealthUrl: http://localhost:5170/api/webhooks/ping
            var webhookUri = new Uri(client.WebhookUrl);
            var pathSegments = webhookUri.AbsolutePath.Split('/');
            var basePath = string.Join("/", pathSegments.Take(pathSegments.Length - 1));
            var healthUrl = $"{webhookUri.Scheme}://{webhookUri.Host}:{webhookUri.Port}{basePath}/ping";

            _logger.LogDebug("Checking health for client {ClientId} at {HealthUrl}", client.Id, healthUrl);

            var response = await httpClient.GetAsync(healthUrl, cancellationToken);
            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Health check successful for client {ClientId} ({ClientName}) in {ResponseTimeMs}ms",
                    client.Id, client.Name, stopwatch.ElapsedMilliseconds);
                return new ClientHealthResult(true, stopwatch.ElapsedMilliseconds, null);
            }
            else
            {
                _logger.LogWarning("Health check failed for client {ClientId} ({ClientName}) with status {StatusCode}",
                    client.Id, client.Name, (int)response.StatusCode);
                return new ClientHealthResult(false, stopwatch.ElapsedMilliseconds, $"HTTP {(int)response.StatusCode}");
            }
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Health check failed for client {ClientId} ({ClientName}): {Error}",
                client.Id, client.Name, ex.Message);
            return new ClientHealthResult(false, stopwatch.ElapsedMilliseconds, ex.Message);
        }
    }
}
