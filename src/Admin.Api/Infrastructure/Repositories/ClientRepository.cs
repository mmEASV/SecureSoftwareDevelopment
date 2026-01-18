using Microsoft.EntityFrameworkCore;
using Admin.Api.Domain.Interfaces;
using Admin.Api.Infrastructure.Persistence;
using Admin.Shared.Models;

namespace Admin.Api.Infrastructure.Repositories;

public class ClientRepository : IClientRepository
{
    private readonly UpdateServiceDbContext _context;

    public ClientRepository(UpdateServiceDbContext context)
    {
        _context = context;
    }

    public async Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Clients
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<List<Client>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Clients.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        return await query
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Client>> GetActiveClientsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Clients
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Client> CreateAsync(Client client, CancellationToken cancellationToken = default)
    {
        _context.Clients.Add(client);
        await _context.SaveChangesAsync(cancellationToken);
        return client;
    }

    public async Task<Client> UpdateAsync(Client client, CancellationToken cancellationToken = default)
    {
        _context.Clients.Update(client);
        await _context.SaveChangesAsync(cancellationToken);
        return client;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var client = await _context.Clients.FindAsync([id], cancellationToken);
        if (client != null)
        {
            _context.Clients.Remove(client);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task UpdateWebhookStatusAsync(Guid id, bool success, CancellationToken cancellationToken = default)
    {
        var client = await _context.Clients.FindAsync([id], cancellationToken);
        if (client != null)
        {
            if (success)
            {
                client.LastWebhookSuccess = DateTime.UtcNow;
                client.ConsecutiveFailures = 0;
            }
            else
            {
                client.LastWebhookFailure = DateTime.UtcNow;
                client.ConsecutiveFailures++;
            }
            client.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
