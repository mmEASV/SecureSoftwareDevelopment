using Microsoft.EntityFrameworkCore;
using Admin.Api.Domain.Interfaces;
using Admin.Api.Infrastructure.Persistence;
using Admin.Shared.Models;

namespace Admin.Api.Infrastructure.Repositories;

public class UpdateRepository : IUpdateRepository
{
    private readonly UpdateServiceDbContext _context;

    public UpdateRepository(UpdateServiceDbContext context)
    {
        _context = context;
    }

    public async Task<Update?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Updates
            .Include(u => u.Releases)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<Update?> GetByVersionAsync(string version, CancellationToken cancellationToken = default)
    {
        return await _context.Updates
            .FirstOrDefaultAsync(u => u.Version == version, cancellationToken);
    }

    public async Task<List<Update>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Updates.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(u => u.IsActive);
        }

        return await query
            .OrderByDescending(u => u.Version)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Update>> GetByDeviceTypeAsync(string deviceType, CancellationToken cancellationToken = default)
    {
        return await _context.Updates
            .Where(u => u.IsActive && u.TargetDeviceTypes.Contains(deviceType))
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Update> CreateAsync(Update update, CancellationToken cancellationToken = default)
    {
        _context.Updates.Add(update);
        await _context.SaveChangesAsync(cancellationToken);
        return update;
    }

    public async Task<Update> UpdateAsync(Update update, CancellationToken cancellationToken = default)
    {
        _context.Updates.Update(update);
        await _context.SaveChangesAsync(cancellationToken);
        return update;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var update = await GetByIdAsync(id, cancellationToken);
        if (update != null)
        {
            _context.Updates.Remove(update);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
