using Admin.Shared.Enums;
using Admin.Shared.Models;

namespace ClientPortal.Api.Domain.Interfaces;

public interface IDeploymentRepository
{
    Task<Deployment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Deployment>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<Deployment>> GetByDeviceIdAsync(Guid deviceId, CancellationToken cancellationToken = default);
    Task<List<Deployment>> GetByReleaseIdAsync(Guid releaseId, CancellationToken cancellationToken = default);
    Task<List<Deployment>> GetByStatusAsync(DeploymentStatus status, CancellationToken cancellationToken = default);
    Task<List<Deployment>> GetPendingForDeviceAsync(Guid deviceId, CancellationToken cancellationToken = default);
    Task<Deployment> CreateAsync(Deployment deployment, CancellationToken cancellationToken = default);
    Task<Deployment> UpdateAsync(Deployment deployment, CancellationToken cancellationToken = default);
    Task<List<Deployment>> CreateBulkAsync(List<Deployment> deployments, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
