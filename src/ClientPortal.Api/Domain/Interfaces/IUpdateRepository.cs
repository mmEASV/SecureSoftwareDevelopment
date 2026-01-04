using Admin.Shared.Models;

namespace ClientPortal.Api.Domain.Interfaces;

public interface IUpdateRepository
{
    Task<Update?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Update?> GetByVersionAsync(string version, CancellationToken cancellationToken = default);
    Task<List<Update>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<List<Update>> GetByDeviceTypeAsync(string deviceType, CancellationToken cancellationToken = default);
    Task<Update> CreateAsync(Update update, CancellationToken cancellationToken = default);
    Task<Update> UpdateAsync(Update update, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
