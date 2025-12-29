using Template.Shared.Dto.User;
using Template.Shared.Models.Identity;
using Template.Shared.Utils;

namespace Template.Api.Application.Common.Interfaces;

public interface IUserService
{
    Task<PaginatedList<ApplicationUser>> Get(BasePaginator paginator, CancellationToken token);
    Task<ApplicationUser?> Get(Guid id);
    Task<ApplicationUser> Create(NewUserDto dto);
    Task<ApplicationUser> Update(Guid id, UpdateUserDto dto);
    Task Delete(Guid id);
}
