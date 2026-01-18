using Admin.Shared.Models;

namespace Admin.Api.Domain.Interfaces;

public interface IClientRepository
{
    Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Client>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<List<Client>> GetActiveClientsAsync(CancellationToken cancellationToken = default);
    Task<Client> CreateAsync(Client client, CancellationToken cancellationToken = default);
    Task<Client> UpdateAsync(Client client, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateWebhookStatusAsync(Guid id, bool success, CancellationToken cancellationToken = default);
}
