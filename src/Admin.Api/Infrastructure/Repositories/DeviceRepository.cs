using Microsoft.EntityFrameworkCore;
using Admin.Api.Domain.Interfaces;
using Admin.Api.Infrastructure.Persistence;
using Admin.Shared.Models;

namespace Admin.Api.Infrastructure.Repositories;

public class DeviceRepository : IDeviceRepository
{
    private readonly UpdateServiceDbContext _context;

    public DeviceRepository(UpdateServiceDbContext context)
    {
        _context = context;
    }

    public async Task<Device?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Devices
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<Device?> GetByIdentifierAsync(string deviceIdentifier, CancellationToken cancellationToken = default)
    {
        return await _context.Devices
            .FirstOrDefaultAsync(d => d.DeviceIdentifier == deviceIdentifier, cancellationToken);
    }

    public async Task<Device?> GetByApiKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        return await _context.Devices
            .FirstOrDefaultAsync(d => d.ApiKey == apiKey, cancellationToken);
    }

    public async Task<List<Device>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Devices.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(d => d.IsActive);
        }

        return await query
            .OrderByDescending(d => d.RegisteredAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Device>> GetByTenantIdAsync(Guid tenantId, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Devices
            .Where(d => d.TenantId == tenantId);

        if (!includeInactive)
        {
            query = query.Where(d => d.IsActive);
        }

        return await query
            .OrderByDescending(d => d.RegisteredAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Device>> GetDevicesWithAutoUpdateAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Devices
            .Where(d => d.AutomaticUpdates && d.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<Device> CreateAsync(Device device, CancellationToken cancellationToken = default)
    {
        _context.Devices.Add(device);
        await _context.SaveChangesAsync(cancellationToken);
        return device;
    }

    public async Task<Device> UpdateAsync(Device device, CancellationToken cancellationToken = default)
    {
        _context.Devices.Update(device);
        await _context.SaveChangesAsync(cancellationToken);
        return device;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var device = await GetByIdAsync(id, cancellationToken);
        if (device != null)
        {
            _context.Devices.Remove(device);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
