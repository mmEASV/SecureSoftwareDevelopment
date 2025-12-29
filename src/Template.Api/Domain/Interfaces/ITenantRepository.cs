using Template.Shared.Models;
using Template.Shared.Utils;

namespace Template.Api.Domain.Interfaces;

public interface ITenantRepository
{
    /// <summary>
    /// Retrieve paginated list of all tenants
    /// </summary>
    /// <param name="paginator"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task<PaginatedList<Tenant>> Get(BasePaginator paginator, CancellationToken token);

    /// <summary>
    /// Retrieve tenant by ID
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<Tenant?> Get(Guid id);

    /// <summary>
    /// Create new tenant
    /// </summary>
    /// <param name="tenant"></param>
    void Create(Tenant tenant);

    /// <summary>
    /// Update existing tenant
    /// </summary>
    /// <param name="tenant"></param>
    void Update(Tenant tenant);

    /// <summary>
    /// Delete existing tenant
    /// </summary>
    /// <param name="tenant"></param>
    void Delete(Tenant tenant);
}
