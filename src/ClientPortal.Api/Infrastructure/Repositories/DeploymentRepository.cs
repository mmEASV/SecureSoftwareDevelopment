using Microsoft.EntityFrameworkCore;
using ClientPortal.Api.Domain.Interfaces;
using ClientPortal.Api.Infrastructure.Persistence;
using Admin.Shared.Enums;
using Admin.Shared.Models;

namespace ClientPortal.Api.Infrastructure.Repositories;

public class DeploymentRepository : IDeploymentRepository
{
    private readonly UpdateServiceDbContext _context;

    public DeploymentRepository(UpdateServiceDbContext context)
    {
        _context = context;
    }

    public async Task<Deployment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Deployments
            .Include(d => d.Release)
                .ThenInclude(r => r.Update)
            .Include(d => d.Device)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<List<Deployment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Deployments
            .Include(d => d.Release)
                .ThenInclude(r => r.Update)
            .Include(d => d.Device)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Deployment>> GetByDeviceIdAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        return await _context.Deployments
            .Include(d => d.Release)
                .ThenInclude(r => r.Update)
            .Where(d => d.DeviceId == deviceId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Deployment>> GetByReleaseIdAsync(Guid releaseId, CancellationToken cancellationToken = default)
    {
        return await _context.Deployments
            .Include(d => d.Device)
            .Where(d => d.ReleaseId == releaseId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Deployment>> GetByStatusAsync(DeploymentStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Deployments
            .Include(d => d.Release)
                .ThenInclude(r => r.Update)
            .Include(d => d.Device)
            .Where(d => d.Status == status)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Deployment>> GetPendingForDeviceAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        return await _context.Deployments
            .Include(d => d.Release)
                .ThenInclude(r => r.Update)
            .Where(d => d.DeviceId == deviceId &&
                   (d.Status == DeploymentStatus.Pending ||
                    d.Status == DeploymentStatus.Downloading ||
                    d.Status == DeploymentStatus.Installing))
            .OrderBy(d => d.ScheduledAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Deployment> CreateAsync(Deployment deployment, CancellationToken cancellationToken = default)
    {
        _context.Deployments.Add(deployment);
        await _context.SaveChangesAsync(cancellationToken);
        return deployment;
    }

    public async Task<Deployment> UpdateAsync(Deployment deployment, CancellationToken cancellationToken = default)
    {
        _context.Deployments.Update(deployment);
        await _context.SaveChangesAsync(cancellationToken);
        return deployment;
    }

    public async Task<List<Deployment>> CreateBulkAsync(List<Deployment> deployments, CancellationToken cancellationToken = default)
    {
        _context.Deployments.AddRange(deployments);
        await _context.SaveChangesAsync(cancellationToken);
        return deployments;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var deployment = await _context.Deployments.FindAsync([id], cancellationToken);
        if (deployment != null)
        {
            _context.Deployments.Remove(deployment);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
