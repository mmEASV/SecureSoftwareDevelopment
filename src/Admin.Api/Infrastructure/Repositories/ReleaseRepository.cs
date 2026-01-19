using Microsoft.EntityFrameworkCore;
using Admin.Api.Domain.Interfaces;
using Admin.Api.Infrastructure.Persistence;
using Admin.Shared.Models;

namespace Admin.Api.Infrastructure.Repositories;

public class ReleaseRepository : IReleaseRepository
{
    private readonly UpdateServiceDbContext _context;

    public ReleaseRepository(UpdateServiceDbContext context)
    {
        _context = context;
    }

    public async Task<Release?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Releases
            .Include(r => r.Update)
            .Include(r => r.Deployments)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<List<Release>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Releases
            .Include(r => r.Update)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(r => r.IsActive);
        }

        return await query
            .OrderByDescending(r => r.ReleaseDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Release>> GetActiveReleasesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Releases
            .Include(r => r.Update)
            .Where(r => r.IsActive)
            .OrderByDescending(r => r.ReleaseDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Release>> GetByUpdateIdAsync(Guid updateId, CancellationToken cancellationToken = default)
    {
        return await _context.Releases
            .Include(r => r.Update)
            .Where(r => r.UpdateId == updateId)
            .OrderByDescending(r => r.ReleaseDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Release> CreateAsync(Release release, CancellationToken cancellationToken = default)
    {
        release.ReleaseDate = DateTime.UtcNow;
        _context.Releases.Add(release);
        await _context.SaveChangesAsync(cancellationToken);
        return release;
    }

    public async Task<Release> UpdateAsync(Release release, CancellationToken cancellationToken = default)
    {
        _context.Releases.Update(release);
        await _context.SaveChangesAsync(cancellationToken);
        return release;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var release = await _context.Releases.FindAsync([id], cancellationToken);
        if (release != null)
        {
            _context.Releases.Remove(release);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
