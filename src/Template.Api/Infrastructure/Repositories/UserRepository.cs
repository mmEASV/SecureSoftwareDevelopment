using Template.Shared.Models;
using Template.Shared.Utils;
using Microsoft.EntityFrameworkCore;
using Template.Api.Domain.Interfaces;
using Template.Api.Infrastructure.Persistence;
using Template.Shared.Models.Identity;

namespace Template.Api.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<ApplicationUser>> Get(BasePaginator paginator, CancellationToken token)
    {
        var query = _context.Users
            .Include(u => u.Owner)
            .AsQueryable();

        return await PaginatedList<ApplicationUser>.CreateAsync(query, paginator.Page, paginator.ItemsPerPage, token);
    }

    public async Task<ApplicationUser?> Get(Guid id)
    {
        return await _context.Users
            .Include(u => u.Owner)
            .SingleOrDefaultAsync(u => u.Id == id);
    }
}
