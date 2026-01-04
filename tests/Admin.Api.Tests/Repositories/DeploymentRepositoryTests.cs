using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Admin.Api.Infrastructure.Persistence;
using Admin.Api.Infrastructure.Repositories;
using Admin.Shared.Enums;
using Admin.Shared.Models;

namespace Admin.Api.Tests.Repositories;

public class DeploymentRepositoryTests : IDisposable
{
    private readonly UpdateServiceDbContext _context;
    private readonly DeploymentRepository _repository;
    private readonly ReleaseRepository _releaseRepository;
    private readonly DeviceRepository _deviceRepository;
    private readonly UpdateRepository _updateRepository;

    public DeploymentRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<UpdateServiceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new UpdateServiceDbContext(options);
        _repository = new DeploymentRepository(_context);
        _releaseRepository = new ReleaseRepository(_context);
        _deviceRepository = new DeviceRepository(_context);
        _updateRepository = new UpdateRepository(_context);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateDeployment_WithPendingStatus()
    {
        // Arrange
        var (release, device) = await CreateTestReleaseAndDevice();
        var deployment = new Deployment
        {
            ReleaseId = release.Id,
            DeviceId = device.Id,
            ScheduledAt = DateTime.UtcNow.AddHours(2),
            Status = DeploymentStatus.Pending
        };

        // Act
        var result = await _repository.CreateAsync(deployment);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Status.Should().Be(DeploymentStatus.Pending);
        result.ScheduledAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByDeviceIdAsync_ShouldReturnDeploymentsForDevice()
    {
        // Arrange
        var (release1, device) = await CreateTestReleaseAndDevice();
        var (release2, _) = await CreateTestReleaseAndDevice();

        await _repository.CreateAsync(new Deployment
        {
            ReleaseId = release1.Id,
            DeviceId = device.Id,
            Status = DeploymentStatus.Pending
        });

        await _repository.CreateAsync(new Deployment
        {
            ReleaseId = release2.Id,
            DeviceId = device.Id,
            Status = DeploymentStatus.Completed
        });

        // Act
        var result = await _repository.GetByDeviceIdAsync(device.Id);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(d => d.DeviceId.Should().Be(device.Id));
    }

    [Fact]
    public async Task GetByReleaseIdAsync_ShouldReturnDeploymentsForRelease()
    {
        // Arrange
        var (release, device1) = await CreateTestReleaseAndDevice();
        var device2 = await CreateTestDevice("DEV-002");

        await _repository.CreateAsync(new Deployment
        {
            ReleaseId = release.Id,
            DeviceId = device1.Id,
            Status = DeploymentStatus.Pending
        });

        await _repository.CreateAsync(new Deployment
        {
            ReleaseId = release.Id,
            DeviceId = device2.Id,
            Status = DeploymentStatus.Downloading
        });

        // Act
        var result = await _repository.GetByReleaseIdAsync(release.Id);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(d => d.ReleaseId.Should().Be(release.Id));
    }

    [Fact]
    public async Task GetByStatusAsync_ShouldReturnDeploymentsWithStatus()
    {
        // Arrange
        var (release1, device1) = await CreateTestReleaseAndDevice();
        var (release2, device2) = await CreateTestReleaseAndDevice();

        await _repository.CreateAsync(new Deployment
        {
            ReleaseId = release1.Id,
            DeviceId = device1.Id,
            Status = DeploymentStatus.Failed,
            ErrorMessage = "Installation failed"
        });

        await _repository.CreateAsync(new Deployment
        {
            ReleaseId = release2.Id,
            DeviceId = device2.Id,
            Status = DeploymentStatus.Completed
        });

        // Act
        var result = await _repository.GetByStatusAsync(DeploymentStatus.Failed);

        // Assert
        result.Should().HaveCount(1);
        result.First().Status.Should().Be(DeploymentStatus.Failed);
        result.First().ErrorMessage.Should().Be("Installation failed");
    }

    [Fact]
    public async Task GetPendingForDeviceAsync_ShouldReturnOnlyPendingDeployments()
    {
        // Arrange
        var (release1, device) = await CreateTestReleaseAndDevice();
        var (release2, _) = await CreateTestReleaseAndDevice();
        var (release3, _) = await CreateTestReleaseAndDevice();

        await _repository.CreateAsync(new Deployment
        {
            ReleaseId = release1.Id,
            DeviceId = device.Id,
            Status = DeploymentStatus.Pending
        });

        await _repository.CreateAsync(new Deployment
        {
            ReleaseId = release2.Id,
            DeviceId = device.Id,
            Status = DeploymentStatus.Downloading
        });

        await _repository.CreateAsync(new Deployment
        {
            ReleaseId = release3.Id,
            DeviceId = device.Id,
            Status = DeploymentStatus.Completed
        });

        // Act
        var result = await _repository.GetPendingForDeviceAsync(device.Id);

        // Assert
        result.Should().HaveCount(2); // Pending and Downloading
        result.Should().NotContain(d => d.Status == DeploymentStatus.Completed);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateDeploymentStatus_WithProgress()
    {
        // Arrange
        var (release, device) = await CreateTestReleaseAndDevice();
        var deployment = new Deployment
        {
            ReleaseId = release.Id,
            DeviceId = device.Id,
            Status = DeploymentStatus.Pending
        };
        await _repository.CreateAsync(deployment);

        // Act - Simulate download progress
        deployment.Status = DeploymentStatus.Downloading;
        deployment.StartedAt = DateTime.UtcNow;
        deployment.DownloadProgress = 50;
        var result = await _repository.UpdateAsync(deployment);

        // Assert
        result.Status.Should().Be(DeploymentStatus.Downloading);
        result.StartedAt.Should().NotBeNull();
        result.DownloadProgress.Should().Be(50);
    }

    [Fact]
    public async Task UpdateAsync_ShouldSupportPostpone_CRACompliance()
    {
        // Arrange
        var (release, device) = await CreateTestReleaseAndDevice();
        var deployment = new Deployment
        {
            ReleaseId = release.Id,
            DeviceId = device.Id,
            Status = DeploymentStatus.Pending,
            ScheduledAt = DateTime.UtcNow
        };
        await _repository.CreateAsync(deployment);

        // Act - Client postpones deployment (CRA opt-out mechanism)
        deployment.Status = DeploymentStatus.Postponed;
        deployment.ScheduledAt = DateTime.UtcNow.AddDays(3);
        deployment.PostponeReason = "Scheduled maintenance window";
        deployment.PostponeCount = 1;
        deployment.LastPostponedAt = DateTime.UtcNow;
        var result = await _repository.UpdateAsync(deployment);

        // Assert
        result.Status.Should().Be(DeploymentStatus.Postponed);
        result.PostponeReason.Should().Be("Scheduled maintenance window");
        result.PostponeCount.Should().Be(1);
        result.LastPostponedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldTrackRetryCount_OnFailure()
    {
        // Arrange
        var (release, device) = await CreateTestReleaseAndDevice();
        var deployment = new Deployment
        {
            ReleaseId = release.Id,
            DeviceId = device.Id,
            Status = DeploymentStatus.Downloading
        };
        await _repository.CreateAsync(deployment);

        // Act - Deployment fails and retries
        deployment.Status = DeploymentStatus.Failed;
        deployment.ErrorMessage = "Network timeout";
        deployment.RetryCount = 1;
        deployment.LastRetryAt = DateTime.UtcNow;
        var result = await _repository.UpdateAsync(deployment);

        // Assert
        result.Status.Should().Be(DeploymentStatus.Failed);
        result.RetryCount.Should().Be(1);
        result.ErrorMessage.Should().Be("Network timeout");
    }

    [Fact]
    public async Task CreateBulkAsync_ShouldCreateMultipleDeployments()
    {
        // Arrange
        var (release, device1) = await CreateTestReleaseAndDevice();
        var device2 = await CreateTestDevice("DEV-BULK-002");
        var device3 = await CreateTestDevice("DEV-BULK-003");

        var deployments = new List<Deployment>
        {
            new() { ReleaseId = release.Id, DeviceId = device1.Id, Status = DeploymentStatus.Pending },
            new() { ReleaseId = release.Id, DeviceId = device2.Id, Status = DeploymentStatus.Pending },
            new() { ReleaseId = release.Id, DeviceId = device3.Id, Status = DeploymentStatus.Pending }
        };

        // Act
        var result = await _repository.CreateBulkAsync(deployments);

        // Assert
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(d =>
        {
            d.Id.Should().NotBeEmpty();
            d.ReleaseId.Should().Be(release.Id);
            d.Status.Should().Be(DeploymentStatus.Pending);
        });
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveDeployment()
    {
        // Arrange
        var (release, device) = await CreateTestReleaseAndDevice();
        var deployment = new Deployment
        {
            ReleaseId = release.Id,
            DeviceId = device.Id,
            Status = DeploymentStatus.Pending
        };
        await _repository.CreateAsync(deployment);

        // Act
        await _repository.DeleteAsync(deployment.Id);

        // Assert
        var result = await _repository.GetByIdAsync(deployment.Id);
        result.Should().BeNull();
    }

    private async Task<(Release release, Device device)> CreateTestReleaseAndDevice()
    {
        var update = await CreateTestUpdate();
        var release = new Release
        {
            UpdateId = update.Id,
            IsActive = true
        };
        await _releaseRepository.CreateAsync(release);

        var device = await CreateTestDevice($"DEV-{Guid.NewGuid().ToString().Substring(0, 8)}");

        return (release, device);
    }

    private async Task<Update> CreateTestUpdate()
    {
        var update = new Update
        {
            Version = $"{Random.Shared.Next(1, 10)}.0.0",
            Title = "Test Update",
            UpdateType = UpdateType.Security,
            Severity = UpdateSeverity.High,
            FilePath = "test.bin",
            FileHash = Guid.NewGuid().ToString(),
            FileSize = 1024
        };
        return await _updateRepository.CreateAsync(update);
    }

    private async Task<Device> CreateTestDevice(string identifier)
    {
        var device = new Device
        {
            DeviceIdentifier = identifier,
            DeviceName = $"Test Device {identifier}",
            DeviceType = "A100",
            TenantId = Guid.NewGuid(),
            CurrentVersion = "1.0.0"
        };
        return await _deviceRepository.CreateAsync(device);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
