using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Admin.Api.Infrastructure.Persistence;
using Admin.Api.Infrastructure.Repositories;
using Admin.Shared.Enums;
using Admin.Shared.Models;

namespace Admin.Api.Tests.Repositories;

public class UpdateRepositoryTests : IDisposable
{
    private readonly UpdateServiceDbContext _context;
    private readonly UpdateRepository _repository;

    public UpdateRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<UpdateServiceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new UpdateServiceDbContext(options);
        _repository = new UpdateRepository(_context);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateUpdate_WhenValidDataProvided()
    {
        // Arrange
        var update = new Update
        {
            Version = "1.0.0",
            Title = "Test Update",
            Description = "Test Description",
            UpdateType = UpdateType.Security,
            Severity = UpdateSeverity.Critical,
            IsSecurityUpdate = true,
            FilePath = "test.bin",
            FileHash = "abc123",
            FileSize = 1024,
            SecurityFixes = new List<string> { "CVE-2024-1234" },
            TargetDeviceTypes = new List<string> { "A100" }
        };

        // Act
        var result = await _repository.CreateAsync(update);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Version.Should().Be("1.0.0");
        result.IsSecurityUpdate.Should().BeTrue();
        result.SecurityFixes.Should().Contain("CVE-2024-1234");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnUpdate_WhenUpdateExists()
    {
        // Arrange
        var update = new Update
        {
            Version = "1.0.1",
            Title = "Another Update",
            UpdateType = UpdateType.BugFix,
            Severity = UpdateSeverity.Medium,
            FilePath = "test2.bin",
            FileHash = "def456",
            FileSize = 2048
        };
        await _repository.CreateAsync(update);

        // Act
        var result = await _repository.GetByIdAsync(update.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Version.Should().Be("1.0.1");
        result.Title.Should().Be("Another Update");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenUpdateDoesNotExist()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByVersionAsync_ShouldReturnUpdate_WhenVersionExists()
    {
        // Arrange
        var update = new Update
        {
            Version = "2.0.0",
            Title = "Version Test",
            UpdateType = UpdateType.Feature,
            Severity = UpdateSeverity.Low,
            FilePath = "test3.bin",
            FileHash = "ghi789",
            FileSize = 3072
        };
        await _repository.CreateAsync(update);

        // Act
        var result = await _repository.GetByVersionAsync("2.0.0");

        // Assert
        result.Should().NotBeNull();
        result!.Version.Should().Be("2.0.0");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllActiveUpdates_WhenIncludeInactiveIsFalse()
    {
        // Arrange
        await _repository.CreateAsync(new Update
        {
            Version = "1.0.0",
            Title = "Active Update",
            IsActive = true,
            UpdateType = UpdateType.Security,
            Severity = UpdateSeverity.High,
            FilePath = "active.bin",
            FileHash = "hash1",
            FileSize = 1000
        });

        await _repository.CreateAsync(new Update
        {
            Version = "2.0.0",
            Title = "Inactive Update",
            IsActive = false,
            UpdateType = UpdateType.BugFix,
            Severity = UpdateSeverity.Low,
            FilePath = "inactive.bin",
            FileHash = "hash2",
            FileSize = 2000
        });

        // Act
        var result = await _repository.GetAllAsync(includeInactive: false);

        // Assert
        result.Should().HaveCount(1);
        result.First().Title.Should().Be("Active Update");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllUpdates_WhenIncludeInactiveIsTrue()
    {
        // Arrange
        await _repository.CreateAsync(new Update
        {
            Version = "1.0.0",
            Title = "Active",
            IsActive = true,
            UpdateType = UpdateType.Security,
            Severity = UpdateSeverity.High,
            FilePath = "test1.bin",
            FileHash = "hash1",
            FileSize = 1000
        });

        await _repository.CreateAsync(new Update
        {
            Version = "2.0.0",
            Title = "Inactive",
            IsActive = false,
            UpdateType = UpdateType.BugFix,
            Severity = UpdateSeverity.Low,
            FilePath = "test2.bin",
            FileHash = "hash2",
            FileSize = 2000
        });

        // Act
        var result = await _repository.GetAllAsync(includeInactive: true);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByDeviceTypeAsync_ShouldReturnMatchingUpdates()
    {
        // Arrange
        await _repository.CreateAsync(new Update
        {
            Version = "1.0.0",
            Title = "A100 Update",
            TargetDeviceTypes = new List<string> { "A100" },
            IsActive = true,
            UpdateType = UpdateType.Security,
            Severity = UpdateSeverity.High,
            FilePath = "a100.bin",
            FileHash = "hash1",
            FileSize = 1000
        });

        await _repository.CreateAsync(new Update
        {
            Version = "2.0.0",
            Title = "B200 Update",
            TargetDeviceTypes = new List<string> { "B200" },
            IsActive = true,
            UpdateType = UpdateType.Feature,
            Severity = UpdateSeverity.Medium,
            FilePath = "b200.bin",
            FileHash = "hash2",
            FileSize = 2000
        });

        // Act
        var result = await _repository.GetByDeviceTypeAsync("A100");

        // Assert
        result.Should().HaveCount(1);
        result.First().Title.Should().Be("A100 Update");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateExistingUpdate()
    {
        // Arrange
        var update = new Update
        {
            Version = "1.0.0",
            Title = "Original Title",
            UpdateType = UpdateType.BugFix,
            Severity = UpdateSeverity.Low,
            FilePath = "test.bin",
            FileHash = "hash",
            FileSize = 1000
        };
        await _repository.CreateAsync(update);

        // Act
        update.Title = "Updated Title";
        update.Description = "New Description";
        var result = await _repository.UpdateAsync(update);

        // Assert
        result.Title.Should().Be("Updated Title");
        result.Description.Should().Be("New Description");

        var retrieved = await _repository.GetByIdAsync(update.Id);
        retrieved!.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveUpdate()
    {
        // Arrange
        var update = new Update
        {
            Version = "1.0.0",
            Title = "To Delete",
            UpdateType = UpdateType.BugFix,
            Severity = UpdateSeverity.Low,
            FilePath = "test.bin",
            FileHash = "hash",
            FileSize = 1000
        };
        await _repository.CreateAsync(update);

        // Act
        await _repository.DeleteAsync(update.Id);

        // Assert
        var result = await _repository.GetByIdAsync(update.Id);
        result.Should().BeNull();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
