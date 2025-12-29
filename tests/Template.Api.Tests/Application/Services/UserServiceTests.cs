using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Template.Api.Domain.Exceptions;
using Template.Api.Domain.Interfaces;
using Template.Api.Application.Common.Mappings;
using Template.Api.Application.Services;
using Template.Api.Infrastructure.Persistence;
using Template.Shared.Dto.User;
using Template.Shared.Models;
using Template.Shared.Models.Identity;
using Template.Shared.Utils;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Template.Api.Tests.Application.Services;

public class UserServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly IUserRepository _userRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IMapper _mapper;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly UserService _service;

    public UserServiceTests()
    {
        // Mock dependencies
        _context = Substitute.For<ApplicationDbContext>();
        _userRepository = Substitute.For<IUserRepository>();
        _tenantRepository = Substitute.For<ITenantRepository>();
        _userManager = Substitute.For<UserManager<ApplicationUser>>(
            Substitute.For<IUserStore<ApplicationUser>>(),
            null, null, null, null, null, null, null, null);

        // Setup mapper with actual mapping configuration
        var myProfile = new MappingProfile();
        var configuration = new MapperConfiguration(cfg => cfg.AddProfile(myProfile));
        _mapper = new Mapper(configuration);

        _service = new UserService(_context, _userRepository, _tenantRepository, _mapper, _userManager);
    }

    [Fact]
    public async Task Get_ReturnsAllUsers()
    {
        // Arrange
        var paginator = new BasePaginator();
        var token = CancellationToken.None;
        var users = new PaginatedList<ApplicationUser>(new List<ApplicationUser>
        {
            new ApplicationUser { Id = Guid.NewGuid(), UserName = "user1@test.com", Email = "user1@test.com", Name = "User", Surname = "One" },
            new ApplicationUser { Id = Guid.NewGuid(), UserName = "user2@test.com", Email = "user2@test.com", Name = "User", Surname = "Two" }
        }, 2, 0, 0, 10);

        _userRepository.Get(paginator, token).Returns(users);

        // Act
        var result = await _service.Get(paginator, token);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("user1@test.com", result[0].Email);
        Assert.Equal("user2@test.com", result[1].Email);

        await _userRepository.Received(1).Get(paginator, token);
    }

    [Fact]
    public async Task Get_WithId_ReturnsUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser { Id = userId, UserName = "test@test.com", Email = "test@test.com", Name = "Test", Surname = "User" };

        _userRepository.Get(userId).Returns(user);

        // Act
        var result = await _service.Get(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal("test@test.com", result.Email);

        await _userRepository.Received(1).Get(userId);
    }

    [Fact]
    public async Task Get_WithInvalidId_ThrowsNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepository.Get(userId).Returns((ApplicationUser?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.Get(userId));
        await _userRepository.Received(1).Get(userId);
    }

    [Fact]
    public async Task Create_WithValidTenant_ReturnsCreatedUser()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var newUserDto = new NewUserDto
        {
            Email = "newuser@test.com",
            Password = "Test@123",
            TenantId = tenantId,
            Name = "Test",
            Surname = "User"
        };

        var tenant = new Tenant { Id = tenantId, Name = "Test Tenant" };
        _tenantRepository.Get(tenantId).Returns(tenant);
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
            .Returns(IdentityResult.Success);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        // Act
        var result = await _service.Create(newUserDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("newuser@test.com", result.Email);
        Assert.Equal("Test", result.Name);
        Assert.Equal("User", result.Surname);

        await _tenantRepository.Received(1).Get(tenantId);
        await _userManager.Received(1).CreateAsync(
            Arg.Is<ApplicationUser>(u => u.Email == "newuser@test.com"),
            "Test@123");
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_WithoutTenant_ReturnsCreatedUser()
    {
        // Arrange
        var newUserDto = new NewUserDto
        {
            Email = "newuser@test.com",
            Password = "Test@123",
            Name = "Test",
            Surname = "User"
        };

        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
            .Returns(IdentityResult.Success);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        // Act
        var result = await _service.Create(newUserDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("newuser@test.com", result.Email);

        await _tenantRepository.DidNotReceive().Get(Arg.Any<Guid>());
        await _userManager.Received(1).CreateAsync(Arg.Any<ApplicationUser>(), "Test@123");
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_WithInvalidTenant_ThrowsBadRequestException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var newUserDto = new NewUserDto
        {
            Email = "newuser@test.com",
            Password = "Test@123",
            TenantId = tenantId
        };

        _tenantRepository.Get(tenantId).Returns((Tenant?)null);

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _service.Create(newUserDto));

        await _tenantRepository.Received(1).Get(tenantId);
        await _userManager.DidNotReceive().CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>());
        await _context.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_WithValidId_ReturnsUpdatedUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateUserDto = new UpdateUserDto
        {
            Email = "updated@test.com",
            Name = "Updated",
            Surname = "User"
        };
        var existingUser = new ApplicationUser
        {
            Id = userId,
            UserName = "original@test.com",
            Email = "original@test.com",
            Name = "Original",
            Surname = "User"
        };

        _userRepository.Get(userId).Returns(existingUser);
        _userManager.UpdateAsync(Arg.Any<ApplicationUser>()).Returns(IdentityResult.Success);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        // Act
        var result = await _service.Update(userId, updateUserDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal("Updated", result.Name);
        Assert.Equal("User", result.Surname);

        await _userRepository.Received(1).Get(userId);
        await _userManager.Received(1).UpdateAsync(Arg.Any<ApplicationUser>());
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_WithInvalidId_ThrowsNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateUserDto = new UpdateUserDto { Email = "updated@test.com" };

        _userRepository.Get(userId).Returns((ApplicationUser?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.Update(userId, updateUserDto));

        await _userRepository.Received(1).Get(userId);
        await _userManager.DidNotReceive().UpdateAsync(Arg.Any<ApplicationUser>());
        await _context.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_WithInvalidTenant_ThrowsBadRequestException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var updateUserDto = new UpdateUserDto
        {
            Email = "updated@test.com",
            TenantId = tenantId
        };
        var existingUser = new ApplicationUser { Id = userId, Email = "original@test.com", Name = "Original", Surname = "User" };

        _userRepository.Get(userId).Returns(existingUser);
        _tenantRepository.Get(tenantId).Returns((Tenant?)null);

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _service.Update(userId, updateUserDto));

        await _userRepository.Received(1).Get(userId);
        await _tenantRepository.Received(1).Get(tenantId);
        await _userManager.DidNotReceive().UpdateAsync(Arg.Any<ApplicationUser>());
    }

    [Fact]
    public async Task Delete_WithValidId_DeletesUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser { Id = userId, Email = "test@test.com", Name = "Test", Surname = "User" };

        _userRepository.Get(userId).Returns(user);
        _userManager.DeleteAsync(Arg.Any<ApplicationUser>()).Returns(IdentityResult.Success);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        // Act
        await _service.Delete(userId);

        // Assert
        await _userRepository.Received(1).Get(userId);
        await _userManager.Received(1).DeleteAsync(Arg.Is<ApplicationUser>(u => u.Id == userId));
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_WithInvalidId_ThrowsNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepository.Get(userId).Returns((ApplicationUser?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.Delete(userId));

        await _userRepository.Received(1).Get(userId);
        await _userManager.DidNotReceive().DeleteAsync(Arg.Any<ApplicationUser>());
        await _context.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Service_SaveChangesFailure_ThrowsException()
    {
        // Arrange
        var newUserDto = new NewUserDto { Email = "new@test.com", Password = "Test@123", Name = "New", Surname = "User" };
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
            .Returns(IdentityResult.Success);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task<int>>(_ => throw new DbUpdateException("Save failed", new Exception()));

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(() => _service.Create(newUserDto));

        await _userManager.Received(1).CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>());
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
