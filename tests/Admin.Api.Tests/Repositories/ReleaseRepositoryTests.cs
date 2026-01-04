using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Admin.Api.Infrastructure.Persistence;
using Admin.Api.Infrastructure.Repositories;
using Admin.Shared.Enums;
using Admin.Shared.Models;

namespace Admin.Api.Tests.Repositories;

public class ReleaseRepositoryTests : IDisposable
{
    private readonly UpdateServiceDbContext _context;
    private readonly ReleaseRepository _repository;
    private readonly UpdateRepository _updateRepository;

    public ReleaseRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<UpdateServiceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new UpdateServiceDbContext(options);
        _repository = new ReleaseRepository(_context);
        _updateRepository = new UpdateRepository(_context);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateRelease_WhenValidDataProvided()
    {
        // Arrange
        var update = await CreateTestUpdate("1.0.0");
        var release = new Release
        {
            UpdateId = update.Id,
            ReleaseDate = DateTime.UtcNow,
            IsActive = true,
            IsMandatory = true,
            MaxPostponeDays = 7,
            ReleaseNotes = "Critical security fix"
        };

        // Act
        var result = await _repository.CreateAsync(release);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.UpdateId.Should().Be(update.Id);
        result.IsMandatory.Should().BeTrue();
        result.MaxPostponeDays.Should().Be(7);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnReleaseWithUpdate_WhenReleaseExists()
    {
        // Arrange
        var update = await CreateTestUpdate("2.0.0");
        var release = new Release
        {
            UpdateId = update.Id,
            ReleaseNotes = "Test Release"
        };
        await _repository.CreateAsync(release);

        // Act
        var result = await _repository.GetByIdAsync(release.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Update.Should().NotBeNull();
        result.Update.Version.Should().Be("2.0.0");
    }

    [Fact]
    public async Task GetActiveReleasesAsync_ShouldReturnOnlyActiveReleases()
    {
        // Arrange
        var update1 = await CreateTestUpdate("1.0.0");
        var update2 = await CreateTestUpdate("2.0.0");

        await _repository.CreateAsync(new Release
        {
            UpdateId = update1.Id,
            IsActive = true,
            ReleaseNotes = "Active Release"
        });

        await _repository.CreateAsync(new Release
        {
            UpdateId = update2.Id,
            IsActive = false,
            ReleaseNotes = "Inactive Release"
        });

        // Act
        var result = await _repository.GetActiveReleasesAsync();

        // Assert
        result.Should().HaveCount(1);
        result.First().ReleaseNotes.Should().Be("Active Release");
    }

    [Fact]
    public async Task GetByUpdateIdAsync_ShouldReturnAllReleasesForUpdate()
    {
        // Arrange
        var update = await CreateTestUpdate("3.0.0");

        await _repository.CreateAsync(new Release
        {
            UpdateId = update.Id,
            ReleaseNotes = "First Release"
        });

        await _repository.CreateAsync(new Release
        {
            UpdateId = update.Id,
            ReleaseNotes = "Second Release"
        });

        // Act
        var result = await _repository.GetByUpdateIdAsync(update.Id);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateExistingRelease()
    {
        // Arrange
        var update = await CreateTestUpdate("4.0.0");
        var release = new Release
        {
            UpdateId = update.Id,
            IsActive = true,
            IsMandatory = false
        };
        await _repository.CreateAsync(release);

        // Act
        release.IsMandatory = true;
        release.MaxPostponeDays = 3;
        var result = await _repository.UpdateAsync(release);

        // Assert
        result.IsMandatory.Should().BeTrue();
        result.MaxPostponeDays.Should().Be(3);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveRelease()
    {
        // Arrange
        var update = await CreateTestUpdate("5.0.0");
        var release = new Release
        {
            UpdateId = update.Id,
            ReleaseNotes = "To Delete"
        };
        await _repository.CreateAsync(release);

        // Act
        await _repository.DeleteAsync(release.Id);

        // Assert
        var result = await _repository.GetByIdAsync(release.Id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldRespectIncludeInactiveParameter()
    {
        // Arrange
        var update1 = await CreateTestUpdate("6.0.0");
        var update2 = await CreateTestUpdate("7.0.0");

        await _repository.CreateAsync(new Release { UpdateId = update1.Id, IsActive = true });
        await _repository.CreateAsync(new Release { UpdateId = update2.Id, IsActive = false });

        // Act
        var activeOnly = await _repository.GetAllAsync(includeInactive: false);
        var all = await _repository.GetAllAsync(includeInactive: true);

        // Assert
        activeOnly.Should().HaveCount(1);
        all.Should().HaveCount(2);
    }

    private async Task<Update> CreateTestUpdate(string version)
    {
        var update = new Update
        {
            Version = version,
            Title = $"Test Update {version}",
            UpdateType = UpdateType.Security,
            Severity = UpdateSeverity.High,
            FilePath = $"{version}.bin",
            FileHash = Guid.NewGuid().ToString(),
            FileSize = 1024
        };
        return await _updateRepository.CreateAsync(update);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
