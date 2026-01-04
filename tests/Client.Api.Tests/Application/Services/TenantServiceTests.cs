using AutoMapper;
using Client.Api.Application.Common.Mappings;
using Client.Api.Application.Services;
using Client.Api.Domain.Common;
using Client.Api.Domain.Exceptions;
using Client.Api.Domain.Interfaces;
using Client.Api.Infrastructure.Persistence;
using Client.Shared.Dto.Tenant;
using Client.Shared.Models;
using Client.Shared.Utils;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Client.Api.Tests.Application.Services;

public class TenantServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantRepository _tenantRepository;
    private readonly CurrentContext _currentContext;
    private readonly IMapper _mapper;
    private readonly TenantService _service;

    public TenantServiceTests()
    {
        // Mock dependencies
        _context = Substitute.For<ApplicationDbContext>();
        _tenantRepository = Substitute.For<ITenantRepository>();
        _currentContext = new CurrentContext();

        // Setup mapper with actual mapping configuration
        var myProfile = new MappingProfile(); // Replace with your actual profile
        var configuration = new MapperConfiguration(cfg => cfg.AddProfile(myProfile));
        _mapper = new Mapper(configuration);

        _service = new TenantService(_context, _tenantRepository, _currentContext, _mapper);
    }

    [Fact]
    public async Task Get_ReturnsAllTenants()
    {
        // Arrange
        var paginator = new BasePaginator();
        var token = CancellationToken.None;
        var tenants = new PaginatedList<Tenant>(new List<Tenant>
        {
            new Tenant { Id = Guid.NewGuid(), Name = "Tenant 1" },
            new Tenant { Id = Guid.NewGuid(), Name = "Tenant 2" }
        }, 2, 0, 0, 10);

        _tenantRepository.Get(paginator, token).Returns(tenants);

        // Act
        var result = await _service.Get(paginator, token);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Tenant 1", result[0].Name);
        Assert.Equal("Tenant 2", result[1].Name);

        await _tenantRepository.Received(1).Get(paginator, token);
    }

    [Fact]
    public async Task Get_WithId_ReturnsTenant()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Test Tenant" };

        _tenantRepository.Get(tenantId).Returns(tenant);

        // Act
        var result = await _service.Get(tenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tenantId, result.Id);
        Assert.Equal("Test Tenant", result.Name);

        await _tenantRepository.Received(1).Get(tenantId); // Called twice due to the check
    }

    [Fact]
    public async Task Get_WithInvalidId_ThrowsNotFoundException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _tenantRepository.Get(tenantId).Returns((Tenant?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.Get(tenantId));
        await _tenantRepository.Received(1).Get(tenantId);
    }

    [Fact]
    public async Task Create_ReturnsSavedTenant()
    {
        // Arrange
        var newTenantDto = new NewTenantDto
        {
            Name = "New Tenant"
        };

        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        // Act
        var result = await _service.Create(newTenantDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Tenant", result.Name);

        _tenantRepository.Received(1).Create(Arg.Is<Tenant>(t =>
            t.Name == "New Tenant"));
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_WithValidId_ReturnsUpdatedTenant()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var updateTenantDto = new UpdateTenantDto
        {
            Name = "Updated Tenant"
        };
        var existingTenant = new Tenant
        {
            Id = tenantId,
            Name = "Original Tenant"
        };

        _tenantRepository.Get(tenantId).Returns(existingTenant);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        // Act
        var result = await _service.Update(tenantId, updateTenantDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tenantId, result.Id);
        Assert.Equal("Updated Tenant", result.Name);

        await _tenantRepository.Received(1).Get(tenantId);
        _tenantRepository.Received(1).Update(Arg.Is<Tenant>(t =>
            t.Id == tenantId &&
            t.Name == "Updated Tenant"));
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_WithInvalidId_ThrowsNotFoundException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var updateTenantDto = new UpdateTenantDto { Name = "Updated Tenant" };

        _tenantRepository.Get(tenantId).Returns((Tenant?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.Update(tenantId, updateTenantDto));

        await _tenantRepository.Received(1).Get(tenantId);
        _tenantRepository.DidNotReceive().Update(Arg.Any<Tenant>());
        await _context.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_WithValidId_DeletesTenant()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Tenant to Delete" };

        _tenantRepository.Get(tenantId).Returns(tenant);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        // Act
        await _service.Delete(tenantId);

        // Assert
        await _tenantRepository.Received(1).Get(tenantId);
        _tenantRepository.Received(1).Delete(Arg.Is<Tenant>(t => t.Id == tenantId));
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_WithInvalidId_ThrowsNotFoundException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _tenantRepository.Get(tenantId).Returns((Tenant?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.Delete(tenantId));

        await _tenantRepository.Received(1).Get(tenantId);
        _tenantRepository.DidNotReceive().Delete(Arg.Any<Tenant>());
        await _context.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Service_SaveChangesFailure_ThrowsException()
    {
        // Arrange
        var newTenantDto = new NewTenantDto { Name = "New Tenant" };
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Throws(new DbUpdateException("Save failed", new Exception()));

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(() => _service.Create(newTenantDto));

        _tenantRepository.Received(1).Create(Arg.Any<Tenant>());
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
