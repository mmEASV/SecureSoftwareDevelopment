using Admin.Shared.Models;

namespace ClientPortal.Api.Domain.Interfaces;

public interface IDeviceRepository
{
    Task<Device?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Device?> GetByIdentifierAsync(string deviceIdentifier, CancellationToken cancellationToken = default);
    Task<Device?> GetByApiKeyAsync(string apiKey, CancellationToken cancellationToken = default);
    Task<List<Device>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<List<Device>> GetByTenantIdAsync(Guid tenantId, bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<List<Device>> GetDevicesWithAutoUpdateAsync(CancellationToken cancellationToken = default);
    Task<Device> CreateAsync(Device device, CancellationToken cancellationToken = default);
    Task<Device> UpdateAsync(Device device, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
