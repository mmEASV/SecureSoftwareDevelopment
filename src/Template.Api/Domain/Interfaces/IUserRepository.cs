using Template.Shared.Models;
using Template.Shared.Models.Identity;
using Template.Shared.Utils;

namespace Template.Api.Domain.Interfaces;

public interface IUserRepository
{
    Task<PaginatedList<ApplicationUser>> Get(BasePaginator paginator, CancellationToken token);
    Task<ApplicationUser?> Get(Guid id);
}
