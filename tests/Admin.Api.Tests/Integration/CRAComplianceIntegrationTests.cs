using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Admin.Api.Infrastructure.Persistence;
using Admin.Api.Infrastructure.Repositories;
using Admin.Shared.Enums;
using Admin.Shared.Models;

namespace Admin.Api.Tests.Integration;

/// <summary>
/// Integration tests to verify CRA (Cyber Resilience Act) compliance features
/// </summary>
public class CRAComplianceIntegrationTests : IDisposable
{
    private readonly UpdateServiceDbContext _context;
    private readonly UpdateRepository _updateRepository;
    private readonly ReleaseRepository _releaseRepository;
    private readonly DeviceRepository _deviceRepository;
    private readonly DeploymentRepository _deploymentRepository;

    public CRAComplianceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<UpdateServiceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new UpdateServiceDbContext(options);
        _updateRepository = new UpdateRepository(_context);
        _releaseRepository = new ReleaseRepository(_context);
        _deviceRepository = new DeviceRepository(_context);
        _deploymentRepository = new DeploymentRepository(_context);
    }

    [Fact]
    public async Task CRA_AutomaticUpdates_ShouldBeEnabledByDefault()
    {
        // CRA Requirement: Automatic security updates must be enabled by default

        // Act - Register a new device
        var device = new Device
        {
            DeviceIdentifier = "AGK-CRA-001",
            DeviceName = "CRA Test Device",
            DeviceType = "A100",
            TenantId = Guid.NewGuid(),
            CurrentVersion = "1.0.0",
            AutomaticUpdates = true // CRA: MUST be true by default
        };
        var created = await _deviceRepository.CreateAsync(device);

        // Assert
        created.AutomaticUpdates.Should().BeTrue("CRA requires automatic updates enabled by default");
    }

    [Fact]
    public async Task CRA_OptOutMechanism_ShouldAllowDisablingAutomaticUpdates()
    {
        // CRA Requirement: Clear and easy opt-out mechanism

        // Arrange - Create device with automatic updates enabled
        var device = new Device
        {
            DeviceIdentifier = "AGK-CRA-002",
            DeviceName = "Opt-Out Test",
            DeviceType = "A100",
            TenantId = Guid.NewGuid(),
            CurrentVersion = "1.0.0",
            AutomaticUpdates = true
        };
        await _deviceRepository.CreateAsync(device);

        // Act - Client opts out of automatic updates
        device.AutomaticUpdates = false;
        var updated = await _deviceRepository.UpdateAsync(device);

        // Assert
        updated.AutomaticUpdates.Should().BeFalse("CRA requires clear opt-out mechanism");
    }

    [Fact]
    public async Task CRA_PostponeUpdates_ShouldAllowTemporaryPostponement()
    {
        // CRA Requirement: Option to temporarily postpone updates

        // Arrange - Create update, release, device, and deployment
        var update = await CreateSecurityUpdate("2.0.0");
        var release = await CreateRelease(update.Id, isMandatory: false);
        var device = await CreateDevice("AGK-CRA-003");
        var deployment = new Deployment
        {
            ReleaseId = release.Id,
            DeviceId = device.Id,
            ScheduledAt = DateTime.UtcNow,
            Status = DeploymentStatus.Pending
        };
        await _deploymentRepository.CreateAsync(deployment);

        // Act - Client postpones the deployment
        deployment.Status = DeploymentStatus.Postponed;
        deployment.ScheduledAt = DateTime.UtcNow.AddDays(3);
        deployment.PostponeReason = "Scheduled maintenance";
        deployment.PostponeCount = 1;
        deployment.LastPostponedAt = DateTime.UtcNow;
        var postponed = await _deploymentRepository.UpdateAsync(deployment);

        // Assert
        postponed.Status.Should().Be(DeploymentStatus.Postponed, "CRA requires ability to postpone updates");
        postponed.PostponeReason.Should().NotBeNullOrEmpty();
        postponed.PostponeCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CRA_MandatorySecurityUpdates_ShouldEnforceMaximumPostponePeriod()
    {
        // CRA Requirement: Mandatory security updates with limited postpone period

        // Arrange - Create mandatory security update
        var update = await CreateSecurityUpdate("3.0.0");
        var release = await CreateRelease(update.Id, isMandatory: true, maxPostponeDays: 7);
        var device = await CreateDevice("AGK-CRA-004");

        var deployment = new Deployment
        {
            ReleaseId = release.Id,
            DeviceId = device.Id,
            ScheduledAt = DateTime.UtcNow,
            Status = DeploymentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        await _deploymentRepository.CreateAsync(deployment);

        // Act - Check postpone constraints
        var maxAllowedPostponeDate = deployment.CreatedAt.AddDays(release.MaxPostponeDays);

        // Assert
        release.IsMandatory.Should().BeTrue("Security updates can be mandatory");
        release.MaxPostponeDays.Should().Be(7, "CRA: Limited postpone period for security updates");
        maxAllowedPostponeDate.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task CRA_SecurityUpdateNotification_ShouldIdentifySecurityUpdates()
    {
        // CRA Requirement: Notification of available security updates

        // Arrange & Act - Create security update with CVE fixes
        var update = new Update
        {
            Version = "4.0.0",
            Title = "Critical Security Patch",
            IsSecurityUpdate = true,
            UpdateType = UpdateType.Security,
            Severity = UpdateSeverity.Critical,
            SecurityFixes = new List<string> { "CVE-2024-1234", "CVE-2024-5678" },
            FilePath = "update-4.0.0.bin",
            FileHash = "hash123",
            FileSize = 1024
        };
        var created = await _updateRepository.CreateAsync(update);

        // Assert
        created.IsSecurityUpdate.Should().BeTrue("CRA: Security updates must be clearly identified");
        created.SecurityFixes.Should().NotBeEmpty("CRA: List of security vulnerabilities fixed");
        created.Severity.Should().Be(UpdateSeverity.Critical);
    }

    [Fact]
    public async Task CRA_UpdateTransparency_ShouldProvideChangelogAndSecurityInfo()
    {
        // CRA Requirement: Transparent information about updates

        // Act - Create update with detailed information
        var update = new Update
        {
            Version = "5.0.0",
            Title = "Security and Feature Update",
            Description = "Addresses critical vulnerabilities and adds new features",
            ChangeLog = "- Fixed authentication bypass\n- Added encryption\n- Performance improvements",
            SecurityFixes = new List<string> { "CVE-2024-9999" },
            IsSecurityUpdate = true,
            UpdateType = UpdateType.Security,
            Severity = UpdateSeverity.High,
            FilePath = "update-5.0.0.bin",
            FileHash = "hash456",
            FileSize = 2048
        };
        var created = await _updateRepository.CreateAsync(update);

        // Assert
        created.ChangeLog.Should().NotBeNullOrEmpty("CRA: Transparent changelog required");
        created.SecurityFixes.Should().NotBeEmpty("CRA: Security vulnerability list required");
        created.Description.Should().NotBeNullOrEmpty("CRA: Clear description required");
    }

    [Fact]
    public async Task CRA_FullWorkflow_SecurityUpdateDeployment()
    {
        // Complete CRA-compliant workflow from update creation to deployment

        // 1. Manufacturer creates security update
        var update = await CreateSecurityUpdate("6.0.0");
        update.IsSecurityUpdate.Should().BeTrue();

        // 2. Manufacturer releases update as mandatory
        var release = await CreateRelease(update.Id, isMandatory: true, maxPostponeDays: 7);
        release.IsMandatory.Should().BeTrue();

        // 3. Device with automatic updates enabled (CRA default)
        var device = await CreateDevice("AGK-CRA-FULL-001");
        device.AutomaticUpdates.Should().BeTrue();

        // 4. Deployment is scheduled
        var deployment = new Deployment
        {
            ReleaseId = release.Id,
            DeviceId = device.Id,
            ScheduledAt = DateTime.UtcNow.AddHours(1),
            Status = DeploymentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        await _deploymentRepository.CreateAsync(deployment);

        // 5. Client attempts to postpone (CRA opt-out)
        var postponeUntil = deployment.CreatedAt.AddDays(5);
        var maxAllowedDate = deployment.CreatedAt.AddDays(release.MaxPostponeDays);

        // Assert - Postpone is allowed within limits
        postponeUntil.Should().BeBefore(maxAllowedDate, "CRA: Postpone must be within allowed period");

        // 6. Deployment proceeds
        deployment.Status = DeploymentStatus.Downloading;
        deployment.StartedAt = DateTime.UtcNow;
        deployment.DownloadProgress = 50;
        await _deploymentRepository.UpdateAsync(deployment);

        deployment.Status = DeploymentStatus.Installing;
        deployment.InstallProgress = 75;
        await _deploymentRepository.UpdateAsync(deployment);

        deployment.Status = DeploymentStatus.Completed;
        deployment.CompletedAt = DateTime.UtcNow;
        await _deploymentRepository.UpdateAsync(deployment);

        // 7. Device version updated
        device.CurrentVersion = update.Version;
        await _deviceRepository.UpdateAsync(device);

        // Final assertions
        deployment.Status.Should().Be(DeploymentStatus.Completed);
        device.CurrentVersion.Should().Be("6.0.0");
    }

    [Fact]
    public async Task CRA_NonSecurityUpdate_ShouldAllowOptOut()
    {
        // Non-security updates should be more flexible

        // Arrange
        var update = new Update
        {
            Version = "7.0.0",
            Title = "Feature Update",
            IsSecurityUpdate = false,
            UpdateType = UpdateType.Feature,
            Severity = UpdateSeverity.Low,
            FilePath = "update-7.0.0.bin",
            FileHash = "hash789",
            FileSize = 1024
        };
        await _updateRepository.CreateAsync(update);

        var device = await CreateDevice("AGK-CRA-007");
        device.SkipNonSecurityUpdates = true;
        await _deviceRepository.UpdateAsync(device);

        // Assert
        update.IsSecurityUpdate.Should().BeFalse();
        device.SkipNonSecurityUpdates.Should().BeTrue("Clients can skip non-security updates");
    }

    // Helper methods
    private async Task<Update> CreateSecurityUpdate(string version)
    {
        var update = new Update
        {
            Version = version,
            Title = $"Security Update {version}",
            IsSecurityUpdate = true,
            UpdateType = UpdateType.Security,
            Severity = UpdateSeverity.Critical,
            SecurityFixes = new List<string> { $"CVE-2024-{Random.Shared.Next(1000, 9999)}" },
            FilePath = $"update-{version}.bin",
            FileHash = Guid.NewGuid().ToString(),
            FileSize = 1024
        };
        return await _updateRepository.CreateAsync(update);
    }

    private async Task<Release> CreateRelease(Guid updateId, bool isMandatory = false, int maxPostponeDays = 7)
    {
        var release = new Release
        {
            UpdateId = updateId,
            IsActive = true,
            IsMandatory = isMandatory,
            MaxPostponeDays = maxPostponeDays,
            ReleaseDate = DateTime.UtcNow
        };
        return await _releaseRepository.CreateAsync(release);
    }

    private async Task<Device> CreateDevice(string identifier)
    {
        var device = new Device
        {
            DeviceIdentifier = identifier,
            DeviceName = $"Test Device {identifier}",
            DeviceType = "A100",
            TenantId = Guid.NewGuid(),
            CurrentVersion = "1.0.0",
            AutomaticUpdates = true // CRA: Default enabled
        };
        return await _deviceRepository.CreateAsync(device);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
