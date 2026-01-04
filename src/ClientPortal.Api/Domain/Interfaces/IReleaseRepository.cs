using Admin.Shared.Models;

namespace ClientPortal.Api.Domain.Interfaces;

public interface IReleaseRepository
{
    Task<Release?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Release>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<List<Release>> GetActiveReleasesAsync(CancellationToken cancellationToken = default);
    Task<List<Release>> GetByUpdateIdAsync(Guid updateId, CancellationToken cancellationToken = default);
    Task<Release> CreateAsync(Release release, CancellationToken cancellationToken = default);
    Task<Release> UpdateAsync(Release release, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
