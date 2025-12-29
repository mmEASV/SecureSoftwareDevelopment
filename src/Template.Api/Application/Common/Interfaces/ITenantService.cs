using Template.Shared.Dto.Tenant;
using Template.Shared.Models;
using Template.Shared.Utils;

namespace Template.Api.Application.Common.Interfaces;

public interface ITenantService
{
    Task<PaginatedList<Tenant>> Get(BasePaginator paginator, CancellationToken token);
    Task<Tenant?> Get(Guid id);
    Task<Tenant> Create(NewTenantDto dto);
    Task<Tenant> Update(Guid id, UpdateTenantDto dto);
    Task Delete(Guid id);
}
