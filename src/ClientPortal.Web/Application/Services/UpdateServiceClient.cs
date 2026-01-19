using System.Net.Http.Json;
using Admin.Shared.Dto;

namespace ClientPortal.Web.Application.Services;

public class UpdateServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UpdateServiceClient> _logger;

    public UpdateServiceClient(HttpClient httpClient, ILogger<UpdateServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    // Releases (Note: Updates are accessed through Releases in ClientPortal.Api)
    public async Task<List<ReleaseDto>> GetActiveReleasesAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<ReleaseDto>>("/api/releases/active") ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching active releases");
            return new();
        }
    }

    public async Task<ReleaseDto?> GetReleaseByIdAsync(Guid id)
    {
        try
        {
            // Get from active releases list and filter
            var releases = await GetActiveReleasesAsync();
            return releases.FirstOrDefault(r => r.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching release {Id}", id);
            return null;
        }
    }

    // Devices
    public async Task<List<DeviceDto>> GetDevicesAsync(Guid? tenantId = null)
    {
        try
        {
            var url = tenantId.HasValue ? $"/api/devices?tenantId={tenantId}" : "/api/devices";
            return await _httpClient.GetFromJsonAsync<List<DeviceDto>>(url) ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching devices");
            return new();
        }
    }

    public async Task<DeviceDto?> GetDeviceByIdAsync(Guid id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<DeviceDto>($"/api/devices/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching device {Id}", id);
            return null;
        }
    }

    public async Task<DeviceDto?> RegisterDeviceAsync(RegisterDeviceDto dto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/devices", dto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<DeviceDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering device");
            return null;
        }
    }

    public async Task<bool> UpdateDeviceSettingsAsync(Guid deviceId, UpdateDeviceSettingsDto dto)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/devices/{deviceId}/settings", dto);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating device settings");
            return false;
        }
    }

    // Deployments
    public async Task<List<DeploymentDto>> GetDeploymentsAsync(Guid? deviceId = null)
    {
        try
        {
            var url = deviceId.HasValue ? $"/api/deployments?deviceId={deviceId}" : "/api/deployments";
            return await _httpClient.GetFromJsonAsync<List<DeploymentDto>>(url) ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching deployments");
            return new();
        }
    }

    public async Task<bool> ScheduleDeploymentAsync(ScheduleDeploymentDto dto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/deployments/schedule", dto);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling deployment");
            return false;
        }
    }

    public async Task<bool> PostponeDeploymentAsync(Guid deploymentId, PostponeDeploymentDto dto)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/deployments/{deploymentId}/postpone", dto);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error postponing deployment");
            return false;
        }
    }

    public async Task<object?> GetDeploymentStatisticsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<object>("/api/deployments/statistics");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching deployment statistics");
            return null;
        }
    }
}
