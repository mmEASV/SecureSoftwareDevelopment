using Client.Api.Domain.Common;
using Client.Api.Infrastructure.Persistence;
using Client.Api.Infrastructure.Repositories;
using Client.Shared.Models;
using Client.Shared.Utils;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Client.Api.Tests.Infrastructure.Repositories;

public class TenantRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly TenantRepository _repository;

    public TenantRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var currentContext = Substitute.For<CurrentContext>();
        var timeProvider = TimeProvider.System;

        _context = new ApplicationDbContext(options, currentContext, timeProvider);
        _repository = new TenantRepository(_context);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var tenants = new List<Tenant>
        {
            new Tenant { Id = Guid.NewGuid(), Name = "Tenant Alpha" },
            new Tenant { Id = Guid.NewGuid(), Name = "Tenant Beta" },
            new Tenant { Id = Guid.NewGuid(), Name = "Tenant Gamma" },
            new Tenant { Id = Guid.NewGuid(), Name = "Tenant Delta" }
        };

        _context.Tenants.AddRange(tenants);
        _context.SaveChanges();
    }

    [Fact]
    public async Task Get_ReturnsPaginatedTenants()
    {
        // Arrange
        var paginator = new BasePaginator { Page = 1, ItemsPerPage = 10 };
        var token = CancellationToken.None;

        // Act
        var result = await _repository.Get(paginator, token);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Count);
        Assert.Equal(4, result.TotalItems);
    }

    [Fact]
    public async Task Get_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var paginator = new BasePaginator { Page = 1, ItemsPerPage = 2 };
        var token = CancellationToken.None;

        // Act
        var result = await _repository.Get(paginator, token);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(4, result.TotalItems);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task Get_SecondPage_ReturnsRemainingTenants()
    {
        // Arrange
        var paginator = new BasePaginator { Page = 2, ItemsPerPage = 2 };
        var token = CancellationToken.None;

        // Act
        var result = await _repository.Get(paginator, token);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(4, result.TotalItems);
    }

    [Fact]
    public async Task Get_LastPage_ReturnsRemainingTenants()
    {
        // Arrange
        var paginator = new BasePaginator { Page = 2, ItemsPerPage = 3 };
        var token = CancellationToken.None;

        // Act
        var result = await _repository.Get(paginator, token);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(4, result.TotalItems);
    }

    [Fact]
    public async Task Get_ById_ReturnsTenant()
    {
        // Arrange
        var existingTenant = await _context.Tenants.FirstAsync(CancellationToken.None);
        var tenantId = existingTenant.Id;

        // Act
        var result = await _repository.Get(tenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tenantId, result.Id);
        Assert.Equal(existingTenant.Name, result.Name);
    }

    [Fact]
    public async Task Get_ById_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.Get(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Create_AddsTenantToContext()
    {
        // Arrange
        var newTenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "New Tenant"
        };

        // Act
        _repository.Create(newTenant);

        // Assert
        var added = _context.Tenants.Local.FirstOrDefault(t => t.Id == newTenant.Id);
        Assert.NotNull(added);
        Assert.Equal("New Tenant", added.Name);
    }

    [Fact]
    public async Task Create_AndSave_PersistsTenant()
    {
        // Arrange
        var newTenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Persisted Tenant"
        };

        // Act
        _repository.Create(newTenant);
        await _context.SaveChangesAsync(CancellationToken.None);

        // Assert
        var persisted = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == newTenant.Id, CancellationToken.None);
        Assert.NotNull(persisted);
        Assert.Equal("Persisted Tenant", persisted.Name);
    }

    [Fact]
    public async Task Update_ModifiesTenant()
    {
        // Arrange
        var existingTenant = await _context.Tenants.FirstAsync(CancellationToken.None);
        existingTenant.Name = "Updated Name";

        // Act
        _repository.Update(existingTenant);
        await _context.SaveChangesAsync(CancellationToken.None);

        // Assert
        var updated = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == existingTenant.Id, CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Equal("Updated Name", updated.Name);
    }

    [Fact]
    public async Task Delete_RemovesTenant()
    {
        // Arrange
        var tenantToDelete = await _context.Tenants.FirstAsync(CancellationToken.None);
        var tenantId = tenantToDelete.Id;

        // Act
        _repository.Delete(tenantToDelete);
        await _context.SaveChangesAsync(CancellationToken.None);

        // Assert
        var deleted = await _context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId, CancellationToken.None);
        Assert.NotNull(deleted);
        Assert.NotNull(deleted.DeletedAt); // Should be soft-deleted
    }

    [Fact]
    public async Task Delete_DecreasesTotalCount()
    {
        // Arrange
        var initialCount = await _context.Tenants.CountAsync(CancellationToken.None);
        var tenantToDelete = await _context.Tenants.FirstAsync(CancellationToken.None);

        // Act
        _repository.Delete(tenantToDelete);
        await _context.SaveChangesAsync(CancellationToken.None);

        // Assert
        var finalCount = await _context.Tenants.CountAsync(CancellationToken.None);
        Assert.Equal(initialCount - 1, finalCount);
    }

    [Fact]
    public async Task Get_WithEmptyDatabase_ReturnsEmptyPaginatedList()
    {
        // Arrange
        _context.Tenants.RemoveRange(_context.Tenants);
        await _context.SaveChangesAsync(CancellationToken.None);

        var paginator = new BasePaginator { Page = 1, ItemsPerPage = 10 };
        var token = CancellationToken.None;

        // Act
        var result = await _repository.Get(paginator, token);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        Assert.Equal(0, result.TotalItems);
    }

    [Fact]
    public async Task Create_MultipleTenants_AllPersisted()
    {
        // Arrange
        var tenant1 = new Tenant { Id = Guid.NewGuid(), Name = "Batch Tenant 1" };
        var tenant2 = new Tenant { Id = Guid.NewGuid(), Name = "Batch Tenant 2" };

        // Act
        _repository.Create(tenant1);
        _repository.Create(tenant2);
        await _context.SaveChangesAsync(CancellationToken.None);

        // Assert
        var persisted1 = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenant1.Id, CancellationToken.None);
        var persisted2 = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenant2.Id, CancellationToken.None);

        Assert.NotNull(persisted1);
        Assert.NotNull(persisted2);
        Assert.Equal("Batch Tenant 1", persisted1.Name);
        Assert.Equal("Batch Tenant 2", persisted2.Name);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
