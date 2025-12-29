using Template.Shared.Models;
using Template.Shared.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Template.Api.Infrastructure.Persistence;

namespace Template.Api.Infrastructure.Initialization;

public class DbInitializer
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DbInitializer> _logger;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public DbInitializer(
        ApplicationDbContext context,
        ILogger<DbInitializer> logger,
        UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
    {
        _context = context;
        _logger = logger;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task InitializeAsync()
    {
        try
        {
            // Create database and apply migrations
            await _context.Database.MigrateAsync();

            // Seed data
            await SeedDataAsync();

            _logger.LogInformation("Database initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initializing the database");
            throw;
        }
    }

    private async Task SeedDataAsync()
    {
        await SeedTenantsAsync();
        await SeedRolesAsync();
        await SeedUsersAsync();

        await _context.SaveChangesAsync();
    }

    #region Tenants

    private async Task SeedTenantsAsync()
    {
        if (_context.Tenants.All(t => t.Id != Guid.Parse("3e76f4ef-a76c-4442-a931-573a00475e3d")))
        {
            var defaultTenant = new Tenant
            {
                Id = Guid.Parse("3e76f4ef-a76c-4442-a931-573a00475e3d"),
                Name = "Default Tenant"
            };

            await _context.Tenants.AddAsync(defaultTenant);
        }
    }

    #endregion

    #region Users

    private async Task SeedUsersAsync()
    {
        if (_context.Users.All(u => u.UserName != "mm@mm"))
        {
            var user = new ApplicationUser
            {
                UserName = "mm@mm",
                Email = "mm@mm",
                OwnerId = Guid.Parse("3e76f4ef-a76c-4442-a931-573a00475e3d"),
                EmailConfirmed = true,
                Name = "M",
                Surname = "M"
            };

            await _userManager.CreateAsync(user, "P@ssw0rd.+");
            await _userManager.AddToRoleAsync(user, "admin");
        }
    }

    #endregion

    #region Roles

    private async Task SeedRolesAsync()
    {
        if (_context.Roles.All(r => r.Name != "admin"))
        {
            var role = new ApplicationRole()
            {
                Id = Guid.Parse("503b36c2-fb9d-469d-8e74-abbb1bfa4ec3"),
                Name = "admin"
            };

            await _roleManager.CreateAsync(role);
        }

        if (_context.Roles.All(r => r.Name != "user"))
        {
            var role = new ApplicationRole()
            {
                Id = Guid.Parse("41c2be74-623d-41a9-8314-cdf15bd4eaca"),
                Name = "user"
            };

            await _roleManager.CreateAsync(role);
        }
    }

    #endregion

}
