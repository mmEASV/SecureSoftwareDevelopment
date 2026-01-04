using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Admin.Api.Infrastructure.Persistence;
using Admin.Api.Infrastructure.Repositories;
using Admin.Shared.Models;

namespace Admin.Api.Tests.Repositories;

public class DeviceRepositoryTests : IDisposable
{
    private readonly UpdateServiceDbContext _context;
    private readonly DeviceRepository _repository;

    public DeviceRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<UpdateServiceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new UpdateServiceDbContext(options);
        _repository = new DeviceRepository(_context);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateDevice_WithAutomaticUpdatesEnabledByDefault()
    {
        // Arrange
        var device = new Device
        {
            DeviceIdentifier = "AGK-A100-001",
            DeviceName = "Production Line A",
            DeviceType = "A100",
            TenantId = Guid.NewGuid(),
            CurrentVersion = "1.0.0",
            AutomaticUpdates = true // CRA compliance: enabled by default
        };

        // Act
        var result = await _repository.CreateAsync(device);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.DeviceIdentifier.Should().Be("AGK-A100-001");
        result.AutomaticUpdates.Should().BeTrue(); // CRA requirement
    }

    [Fact]
    public async Task GetByIdentifierAsync_ShouldReturnDevice_WhenDeviceExists()
    {
        // Arrange
        var device = new Device
        {
            DeviceIdentifier = "AGK-B200-002",
            DeviceName = "Test Device",
            DeviceType = "B200",
            TenantId = Guid.NewGuid(),
            CurrentVersion = "2.0.0"
        };
        await _repository.CreateAsync(device);

        // Act
        var result = await _repository.GetByIdentifierAsync("AGK-B200-002");

        // Assert
        result.Should().NotBeNull();
        result!.DeviceName.Should().Be("Test Device");
    }

    [Fact]
    public async Task GetByTenantIdAsync_ShouldReturnOnlyDevicesForTenant()
    {
        // Arrange
        var tenant1 = Guid.NewGuid();
        var tenant2 = Guid.NewGuid();

        await _repository.CreateAsync(new Device
        {
            DeviceIdentifier = "TENANT1-001",
            DeviceName = "Tenant 1 Device 1",
            DeviceType = "A100",
            TenantId = tenant1,
            CurrentVersion = "1.0.0"
        });

        await _repository.CreateAsync(new Device
        {
            DeviceIdentifier = "TENANT1-002",
            DeviceName = "Tenant 1 Device 2",
            DeviceType = "A100",
            TenantId = tenant1,
            CurrentVersion = "1.0.0"
        });

        await _repository.CreateAsync(new Device
        {
            DeviceIdentifier = "TENANT2-001",
            DeviceName = "Tenant 2 Device",
            DeviceType = "B200",
            TenantId = tenant2,
            CurrentVersion = "2.0.0"
        });

        // Act
        var result = await _repository.GetByTenantIdAsync(tenant1);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(d => d.TenantId.Should().Be(tenant1));
    }

    [Fact]
    public async Task GetByTenantIdAsync_ShouldRespectIncludeInactiveParameter()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        await _repository.CreateAsync(new Device
        {
            DeviceIdentifier = "ACTIVE-001",
            DeviceName = "Active Device",
            DeviceType = "A100",
            TenantId = tenantId,
            IsActive = true,
            CurrentVersion = "1.0.0"
        });

        await _repository.CreateAsync(new Device
        {
            DeviceIdentifier = "INACTIVE-001",
            DeviceName = "Inactive Device",
            DeviceType = "A100",
            TenantId = tenantId,
            IsActive = false,
            CurrentVersion = "1.0.0"
        });

        // Act
        var activeOnly = await _repository.GetByTenantIdAsync(tenantId, includeInactive: false);
        var all = await _repository.GetByTenantIdAsync(tenantId, includeInactive: true);

        // Assert
        activeOnly.Should().HaveCount(1);
        all.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateDeviceSettings_CRACompliance()
    {
        // Arrange
        var device = new Device
        {
            DeviceIdentifier = "AGK-TEST-001",
            DeviceName = "Test Device",
            DeviceType = "A100",
            TenantId = Guid.NewGuid(),
            CurrentVersion = "1.0.0",
            AutomaticUpdates = true
        };
        await _repository.CreateAsync(device);

        // Act - Client opts out of automatic updates (CRA opt-out mechanism)
        device.AutomaticUpdates = false;
        device.PostponeSecurityUpdates = true;
        device.MaintenanceWindowStart = "22:00";
        device.MaintenanceWindowEnd = "04:00";
        var result = await _repository.UpdateAsync(device);

        // Assert
        result.AutomaticUpdates.Should().BeFalse(); // CRA opt-out
        result.PostponeSecurityUpdates.Should().BeTrue();
        result.MaintenanceWindowStart.Should().Be("22:00");
        result.MaintenanceWindowEnd.Should().Be("04:00");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateCurrentVersion_AfterDeployment()
    {
        // Arrange
        var device = new Device
        {
            DeviceIdentifier = "AGK-VERSION-001",
            DeviceName = "Version Test",
            DeviceType = "A100",
            TenantId = Guid.NewGuid(),
            CurrentVersion = "1.0.0"
        };
        await _repository.CreateAsync(device);

        // Act - Simulate successful update deployment
        device.CurrentVersion = "2.0.0";
        device.LastSeenAt = DateTime.UtcNow;
        var result = await _repository.UpdateAsync(device);

        // Assert
        result.CurrentVersion.Should().Be("2.0.0");
        result.LastSeenAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveDevice()
    {
        // Arrange
        var device = new Device
        {
            DeviceIdentifier = "AGK-DELETE-001",
            DeviceName = "To Delete",
            DeviceType = "A100",
            TenantId = Guid.NewGuid(),
            CurrentVersion = "1.0.0"
        };
        await _repository.CreateAsync(device);

        // Act
        await _repository.DeleteAsync(device.Id);

        // Assert
        var result = await _repository.GetByIdAsync(device.Id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllDevices()
    {
        // Arrange
        await _repository.CreateAsync(new Device
        {
            DeviceIdentifier = "DEV-001",
            DeviceName = "Device 1",
            DeviceType = "A100",
            TenantId = Guid.NewGuid(),
            IsActive = true,
            CurrentVersion = "1.0.0"
        });

        await _repository.CreateAsync(new Device
        {
            DeviceIdentifier = "DEV-002",
            DeviceName = "Device 2",
            DeviceType = "B200",
            TenantId = Guid.NewGuid(),
            IsActive = false,
            CurrentVersion = "2.0.0"
        });

        // Act
        var activeOnly = await _repository.GetAllAsync(includeInactive: false);
        var all = await _repository.GetAllAsync(includeInactive: true);

        // Assert
        activeOnly.Should().HaveCount(1);
        all.Should().HaveCount(2);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
