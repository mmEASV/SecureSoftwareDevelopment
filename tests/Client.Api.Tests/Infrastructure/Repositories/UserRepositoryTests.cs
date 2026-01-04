using Client.Api.Domain.Common;
using Client.Api.Infrastructure.Persistence;
using Client.Api.Infrastructure.Repositories;
using Client.Shared.Models;
using Client.Shared.Models.Identity;
using Client.Shared.Utils;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Client.Api.Tests.Infrastructure.Repositories;

public class UserRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UserRepository _repository;
    private readonly Guid _tenantId = Guid.Parse("3e76f4ef-a76c-4442-a931-573a00475e3d");

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var currentContext = Substitute.For<CurrentContext>();
        var timeProvider = TimeProvider.System;

        _context = new ApplicationDbContext(options, currentContext, timeProvider);
        _repository = new UserRepository(_context);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var tenant = new Tenant
        {
            Id = _tenantId,
            Name = "Test Tenant"
        };

        var users = new List<ApplicationUser>
        {
            new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "user1@test.com",
                Email = "user1@test.com",
                OwnerId = _tenantId,
                Name = "User",
                Surname = "One"
            },
            new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "user2@test.com",
                Email = "user2@test.com",
                OwnerId = _tenantId,
                Name = "User",
                Surname = "Two"
            },
            new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "user3@test.com",
                Email = "user3@test.com",
                OwnerId = _tenantId,
                Name = "User",
                Surname = "Three"
            }
        };

        _context.Tenants.Add(tenant);
        _context.Users.AddRange(users);
        _context.SaveChanges();
    }

    [Fact]
    public async Task Get_ReturnsPaginatedUsers()
    {
        // Arrange
        var paginator = new BasePaginator { Page = 1, ItemsPerPage = 10 };
        var token = CancellationToken.None;

        // Act
        var result = await _repository.Get(paginator, token);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal(3, result.TotalItems);
        Assert.True(result.All(u => u.Email!.Contains("@test.com")));
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
        Assert.Equal(3, result.TotalItems);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task Get_SecondPage_ReturnsRemainingUsers()
    {
        // Arrange
        var paginator = new BasePaginator { Page = 2, ItemsPerPage = 2 };
        var token = CancellationToken.None;

        // Act
        var result = await _repository.Get(paginator, token);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(3, result.TotalItems);
    }

    [Fact]
    public async Task Get_IncludesOwnerTenant()
    {
        // Arrange
        var paginator = new BasePaginator { Page = 1, ItemsPerPage = 10 };
        var token = CancellationToken.None;

        // Act
        var result = await _repository.Get(paginator, token);

        // Assert
        Assert.NotNull(result);
        Assert.All(result, user =>
        {
            Assert.NotNull(user.Owner);
            Assert.Equal("Test Tenant", user.Owner.Name);
        });
    }

    [Fact]
    public async Task Get_ById_ReturnsUser()
    {
        // Arrange
        var existingUser = await _context.Users.FirstAsync(CancellationToken.None);
        var userId = existingUser.Id;

        // Act
        var result = await _repository.Get(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal(existingUser.Email, result.Email);
    }

    [Fact]
    public async Task Get_ById_IncludesOwnerTenant()
    {
        // Arrange
        var existingUser = await _context.Users.FirstAsync(CancellationToken.None);
        var userId = existingUser.Id;

        // Act
        var result = await _repository.Get(userId);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Owner);
        Assert.Equal("Test Tenant", result.Owner.Name);
        Assert.Equal(_tenantId, result.Owner.Id);
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
    public async Task Get_WithEmptyDatabase_ReturnsEmptyPaginatedList()
    {
        // Arrange
        // Clear all users
        _context.Users.RemoveRange(_context.Users);
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

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
