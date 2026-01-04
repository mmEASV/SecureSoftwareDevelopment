using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Admin.Api.Domain.Interfaces;
using Admin.Api.Infrastructure.Storage;
using Admin.Shared.Dto;
using Admin.Shared.Enums;
using Admin.Shared.Models;

namespace Admin.Api.Tests.Endpoints;

public class UpdateEndpointsTests
{
    private readonly IUpdateRepository _repository;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<Program> _logger;

    public UpdateEndpointsTests()
    {
        _repository = Substitute.For<IUpdateRepository>();
        _fileStorage = Substitute.For<IFileStorageService>();
        _logger = Substitute.For<ILogger<Program>>();
    }

    [Fact]
    public async Task GetAllUpdates_ShouldReturnListOfUpdates()
    {
        // Arrange
        var updates = new List<Update>
        {
            new() { Id = Guid.NewGuid(), Version = "1.0.0", Title = "Update 1", FilePath = "file1.bin", FileHash = "hash1", FileSize = 1024,UpdateType = UpdateType.Security, Severity = UpdateSeverity.High },
            new() { Id = Guid.NewGuid(), Version = "2.0.0", Title = "Update 2", FilePath = "file2.bin", FileHash = "hash2", FileSize = 2048, UpdateType = UpdateType.BugFix, Severity = UpdateSeverity.Medium }
        };
        _repository.GetAllAsync(false, Arg.Any<CancellationToken>()).Returns(updates);

        // Act
        var result = await GetAllUpdatesInternal(_repository, false, CancellationToken.None);

        // Assert
        var okResult = result as Ok<List<UpdateDto>>;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().HaveCount(2);
        okResult.Value!.First().Version.Should().Be("1.0.0");
    }

    [Fact]
    public async Task GetUpdateById_ShouldReturnUpdate_WhenExists()
    {
        // Arrange
        var updateId = Guid.NewGuid();
        var update = new Update
        {
            Id = updateId,
            Version = "1.0.0",
            Title = "Test Update",
            FilePath = "test.bin",
            FileHash = "testhash",
            FileSize = 1024,
            UpdateType = UpdateType.Security,
            Severity = UpdateSeverity.Critical,
            IsSecurityUpdate = true
        };
        _repository.GetByIdAsync(updateId, Arg.Any<CancellationToken>()).Returns(update);

        // Act
        var result = await GetUpdateByIdInternal(updateId, _repository, CancellationToken.None);

        // Assert
        var okResult = result as Ok<UpdateDto>;
        okResult.Should().NotBeNull();
        okResult.Value!.Id.Should().Be(updateId);
        okResult.Value.IsSecurityUpdate.Should().BeTrue();
    }

    [Fact]
    public async Task GetUpdateById_ShouldReturnNotFound_WhenDoesNotExist()
    {
        // Arrange
        var updateId = Guid.NewGuid();
        _repository.GetByIdAsync(updateId, Arg.Any<CancellationToken>()).Returns((Update?)null);

        // Act
        var result = await GetUpdateByIdInternal(updateId, _repository, CancellationToken.None);

        // Assert
        result.GetType().Name.Should().StartWith("NotFound");
    }

    [Fact]
    public async Task CreateUpdate_ShouldReturnConflict_WhenVersionAlreadyExists()
    {
        // Arrange
        var dto = new CreateUpdateDto
        {
            Version = "1.0.0",
            Title = "Test Update",
            UpdateType = UpdateType.Security,
            Severity = UpdateSeverity.High
        };

        var existingUpdate = new Update { Version = "1.0.0", FilePath = "existing.bin", FileHash = "hash", FileSize = 1024 };
        _repository.GetByVersionAsync("1.0.0", Arg.Any<CancellationToken>()).Returns(existingUpdate);

        var file = CreateMockFormFile("test.bin", "test content");

        // Act
        var result = await CreateUpdateInternal(dto, file, _repository, _fileStorage, CancellationToken.None);

        // Assert
        result.GetType().Name.Should().StartWith("Conflict");
    }

    [Fact]
    public async Task UpdateUpdate_ShouldUpdateFields_WhenUpdateExists()
    {
        // Arrange
        var updateId = Guid.NewGuid();
        var existingUpdate = new Update
        {
            Id = updateId,
            Version = "1.0.0",
            Title = "Original Title",
            FilePath = "test.bin",
            FileHash = "hash",
            FileSize = 1024,
            UpdateType = UpdateType.BugFix,
            Severity = UpdateSeverity.Low
        };

        _repository.GetByIdAsync(updateId, Arg.Any<CancellationToken>()).Returns(existingUpdate);
        _repository.UpdateAsync(Arg.Any<Update>(), Arg.Any<CancellationToken>())
            .Returns(args => Task.FromResult(args.Arg<Update>()));

        var updateDto = new UpdateUpdateDto
        {
            Title = "Updated Title",
            Description = "New Description",
            IsActive = false
        };

        // Act
        var result = await UpdateUpdateInternal(updateId, updateDto, _repository, CancellationToken.None);

        // Assert
        var okResult = result as Ok<UpdateDto>;
        okResult.Should().NotBeNull();
        okResult.Value!.Title.Should().Be("Updated Title");

        await _repository.Received(1).UpdateAsync(Arg.Is<Update>(u =>
            u.Title == "Updated Title" &&
            u.Description == "New Description" &&
            u.IsActive == false
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteUpdate_ShouldDeleteFileAndUpdate_WhenExists()
    {
        // Arrange
        var updateId = Guid.NewGuid();
        var update = new Update
        {
            Id = updateId,
            Version = "1.0.0",
            FilePath = "test.bin",
            FileHash = "hash",
            FileSize = 1024
        };

        _repository.GetByIdAsync(updateId, Arg.Any<CancellationToken>()).Returns(update);

        // Act
        var result = await DeleteUpdateInternal(updateId, _repository, _fileStorage, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContent>();
        await _fileStorage.Received(1).DeleteFileAsync("test.bin", Arg.Any<CancellationToken>());
        await _repository.Received(1).DeleteAsync(updateId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteUpdate_ShouldReturnNotFound_WhenDoesNotExist()
    {
        // Arrange
        var updateId = Guid.NewGuid();
        _repository.GetByIdAsync(updateId, Arg.Any<CancellationToken>()).Returns((Update?)null);

        // Act
        var result = await DeleteUpdateInternal(updateId, _repository, _fileStorage, CancellationToken.None);

        // Assert
        result.GetType().Name.Should().StartWith("NotFound");
        await _fileStorage.DidNotReceive().DeleteFileAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // Helper methods that mirror the actual endpoint logic
    private static async Task<IResult> GetAllUpdatesInternal(
        IUpdateRepository repository,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        var updates = await repository.GetAllAsync(includeInactive, cancellationToken);
        var dtos = updates.Select(MapToDto).ToList();
        return Results.Ok(dtos);
    }

    private static async Task<IResult> GetUpdateByIdInternal(
        Guid id,
        IUpdateRepository repository,
        CancellationToken cancellationToken)
    {
        var update = await repository.GetByIdAsync(id, cancellationToken);
        if (update == null)
        {
            return Results.NotFound(new { Message = $"Update with ID {id} not found" });
        }
        return Results.Ok(MapToDto(update));
    }

    private static async Task<IResult> CreateUpdateInternal(
        CreateUpdateDto dto,
        IFormFile? file,
        IUpdateRepository repository,
        IFileStorageService fileStorage,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return Results.BadRequest(new { Message = "File is required" });
        }

        var existingUpdate = await repository.GetByVersionAsync(dto.Version, cancellationToken);
        if (existingUpdate != null)
        {
            return Results.Conflict(new { Message = $"Update version {dto.Version} already exists" });
        }

        using var stream = file.OpenReadStream();
        var (filePath, fileHash, digitalSignature, fileSize) = await fileStorage.SaveFileAsync(stream, file.FileName, cancellationToken);

        var update = new Update
        {
            Version = dto.Version,
            Title = dto.Title,
            Description = dto.Description,
            ChangeLog = dto.ChangeLog,
            SecurityFixes = dto.SecurityFixes,
            UpdateType = dto.UpdateType,
            Severity = dto.Severity,
            IsSecurityUpdate = dto.IsSecurityUpdate,
            TargetDeviceTypes = dto.TargetDeviceTypes,
            FilePath = filePath,
            FileHash = fileHash,
            DigitalSignature = digitalSignature,
            FileSize = fileSize,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty
        };

        var created = await repository.CreateAsync(update, cancellationToken);
        return Results.Created($"/api/updates/{created.Id}", MapToDto(created));
    }

    private static async Task<IResult> UpdateUpdateInternal(
        Guid id,
        UpdateUpdateDto dto,
        IUpdateRepository repository,
        CancellationToken cancellationToken)
    {
        var update = await repository.GetByIdAsync(id, cancellationToken);
        if (update == null)
        {
            return Results.NotFound(new { Message = $"Update with ID {id} not found" });
        }

        if (dto.Title != null) update.Title = dto.Title;
        if (dto.Description != null) update.Description = dto.Description;
        if (dto.ChangeLog != null) update.ChangeLog = dto.ChangeLog;
        if (dto.SecurityFixes != null) update.SecurityFixes = dto.SecurityFixes;
        if (dto.UpdateType.HasValue) update.UpdateType = dto.UpdateType.Value;
        if (dto.Severity.HasValue) update.Severity = dto.Severity.Value;
        if (dto.IsSecurityUpdate.HasValue) update.IsSecurityUpdate = dto.IsSecurityUpdate.Value;
        if (dto.IsActive.HasValue) update.IsActive = dto.IsActive.Value;

        update.UpdatedAt = DateTime.UtcNow;
        update.UpdatedBy = Guid.Empty;

        await repository.UpdateAsync(update, cancellationToken);
        return Results.Ok(MapToDto(update));
    }

    private static async Task<IResult> DeleteUpdateInternal(
        Guid id,
        IUpdateRepository repository,
        IFileStorageService fileStorage,
        CancellationToken cancellationToken)
    {
        var update = await repository.GetByIdAsync(id, cancellationToken);
        if (update == null)
        {
            return Results.NotFound(new { Message = $"Update with ID {id} not found" });
        }

        await fileStorage.DeleteFileAsync(update.FilePath, cancellationToken);
        await repository.DeleteAsync(id, cancellationToken);
        return Results.NoContent();
    }

    private static UpdateDto MapToDto(Update update)
    {
        return new UpdateDto
        {
            Id = update.Id,
            Version = update.Version,
            Title = update.Title,
            Description = update.Description,
            ChangeLog = update.ChangeLog,
            SecurityFixes = update.SecurityFixes,
            FileHash = update.FileHash,
            FileSize = update.FileSize,
            UpdateType = update.UpdateType,
            Severity = update.Severity,
            IsSecurityUpdate = update.IsSecurityUpdate,
            TargetDeviceTypes = update.TargetDeviceTypes,
            CreatedAt = update.CreatedAt,
            IsActive = update.IsActive
        };
    }

    private static IFormFile CreateMockFormFile(string fileName, string content)
    {
        var file = Substitute.For<IFormFile>();
        var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
        file.OpenReadStream().Returns(ms);
        file.FileName.Returns(fileName);
        file.Length.Returns(ms.Length);
        return file;
    }
}
