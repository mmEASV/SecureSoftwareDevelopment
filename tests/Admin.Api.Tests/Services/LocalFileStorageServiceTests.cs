using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Admin.Api.Infrastructure.Storage;
using Admin.Api.Infrastructure.Security;

namespace Admin.Api.Tests.Services;

public class LocalFileStorageServiceTests : IDisposable
{
    private readonly LocalFileStorageService _service;
    private readonly string _testStoragePath;
    private readonly ILogger<LocalFileStorageService> _logger;
    private readonly IDigitalSignatureService _signatureService;

    public LocalFileStorageServiceTests()
    {
        _testStoragePath = Path.Combine(Path.GetTempPath(), $"test-storage-{Guid.NewGuid()}");

        var configuration = Substitute.For<IConfiguration>();
        configuration["FileStorage:Path"].Returns(_testStoragePath);

        _logger = Substitute.For<ILogger<LocalFileStorageService>>();

        _signatureService = Substitute.For<IDigitalSignatureService>();
        _signatureService.SignHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("test-signature-base64==");

        _service = new LocalFileStorageService(configuration, _logger, _signatureService);
    }

    [Fact]
    public async Task SaveFileAsync_ShouldSaveFileAndComputeHash()
    {
        // Arrange
        var testContent = "Test file content for hashing"u8.ToArray();
        using var stream = new MemoryStream(testContent);
        var fileName = "test-update.bin";

        // Act
        var (filePath, fileHash, digitalSignature, fileSize) = await _service.SaveFileAsync(stream, fileName);

        // Assert
        filePath.Should().NotBeNullOrEmpty();
        fileHash.Should().NotBeNullOrEmpty();
        fileHash.Length.Should().Be(64); // SHA256 produces 64 hex characters
        digitalSignature.Should().NotBeNullOrEmpty();
        digitalSignature.Should().Be("test-signature-base64==");
        fileSize.Should().Be(testContent.Length);

        // Verify file exists
        var fullPath = Path.Combine(_testStoragePath, filePath);
        File.Exists(fullPath).Should().BeTrue();
    }

    [Fact]
    public async Task SaveFileAsync_ShouldGenerateUniqueFileNames()
    {
        // Arrange
        var content1 = "Content 1"u8.ToArray();
        var content2 = "Content 2"u8.ToArray();

        using var stream1 = new MemoryStream(content1);
        using var stream2 = new MemoryStream(content2);

        // Act
        var (filePath1, _, _, _) = await _service.SaveFileAsync(stream1, "update.bin");
        var (filePath2, _, _, _) = await _service.SaveFileAsync(stream2, "update.bin");

        // Assert
        filePath1.Should().NotBe(filePath2); // Different files even with same original name
    }

    [Fact]
    public async Task SaveFileAsync_ShouldComputeCorrectSHA256Hash()
    {
        // Arrange
        var testContent = "Known content for hash verification"u8.ToArray();
        using var stream = new MemoryStream(testContent);

        // Calculate expected hash
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var expectedHash = Convert.ToHexString(sha256.ComputeHash(testContent)).ToLowerInvariant();

        // Act
        var (_, actualHash, _, _) = await _service.SaveFileAsync(stream, "test.bin");

        // Assert
        actualHash.Should().Be(expectedHash);
    }

    [Fact]
    public async Task GetFileAsync_ShouldRetrieveSavedFile()
    {
        // Arrange
        var testContent = "Test content to retrieve"u8.ToArray();
        using var saveStream = new MemoryStream(testContent);
        var (filePath, _, _, _) = await _service.SaveFileAsync(saveStream, "retrieve-test.bin");

        // Act
        var retrievedStream = await _service.GetFileAsync(filePath);

        // Assert
        retrievedStream.Should().NotBeNull();
        using var ms = new MemoryStream();
        await retrievedStream!.CopyToAsync(ms);
        ms.ToArray().Should().Equal(testContent);
    }

    [Fact]
    public async Task GetFileAsync_ShouldReturnNull_WhenFileDoesNotExist()
    {
        // Act
        var result = await _service.GetFileAsync("non-existent-file.bin");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteFileAsync_ShouldRemoveFile()
    {
        // Arrange
        var testContent = "Content to delete"u8.ToArray();
        using var stream = new MemoryStream(testContent);
        var (filePath, _, _, _) = await _service.SaveFileAsync(stream, "delete-test.bin");

        // Act
        await _service.DeleteFileAsync(filePath);

        // Assert
        var exists = await _service.FileExistsAsync(filePath);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task FileExistsAsync_ShouldReturnTrue_WhenFileExists()
    {
        // Arrange
        var testContent = "Exists test"u8.ToArray();
        using var stream = new MemoryStream(testContent);
        var (filePath, _, _, _) = await _service.SaveFileAsync(stream, "exists-test.bin");

        // Act
        var exists = await _service.FileExistsAsync(filePath);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task FileExistsAsync_ShouldReturnFalse_WhenFileDoesNotExist()
    {
        // Act
        var exists = await _service.FileExistsAsync("non-existent.bin");

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyFileHashAsync_ShouldReturnTrue_WhenHashMatches()
    {
        // Arrange
        var testContent = "Hash verification content"u8.ToArray();
        using var stream = new MemoryStream(testContent);
        var (filePath, expectedHash, _, _) = await _service.SaveFileAsync(stream, "verify-test.bin");

        // Act
        var isValid = await _service.VerifyFileHashAsync(filePath, expectedHash);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyFileHashAsync_ShouldReturnFalse_WhenHashDoesNotMatch()
    {
        // Arrange
        var testContent = "Original content"u8.ToArray();
        using var stream = new MemoryStream(testContent);
        var (filePath, _, _, _) = await _service.SaveFileAsync(stream, "mismatch-test.bin");
        var wrongHash = "0000000000000000000000000000000000000000000000000000000000000000";

        // Act
        var isValid = await _service.VerifyFileHashAsync(filePath, wrongHash);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyFileHashAsync_ShouldReturnFalse_WhenFileDoesNotExist()
    {
        // Act
        var isValid = await _service.VerifyFileHashAsync("non-existent.bin", "somehash");

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task SaveFileAsync_ShouldHandleLargeFiles()
    {
        // Arrange - Create a 1MB file
        var largeContent = new byte[1024 * 1024]; // 1 MB
        Random.Shared.NextBytes(largeContent);
        using var stream = new MemoryStream(largeContent);

        // Act
        var (filePath, fileHash, digitalSignature, fileSize) = await _service.SaveFileAsync(stream, "large-file.bin");

        // Assert
        fileSize.Should().Be(largeContent.Length);
        fileHash.Should().NotBeNullOrEmpty();

        var exists = await _service.FileExistsAsync(filePath);
        exists.Should().BeTrue();
    }

    public void Dispose()
    {
        // Cleanup test storage directory
        if (Directory.Exists(_testStoragePath))
        {
            Directory.Delete(_testStoragePath, recursive: true);
        }
    }
}
