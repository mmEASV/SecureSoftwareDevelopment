namespace Admin.Api.Infrastructure.Storage;

public interface IFileStorageService
{
    /// <summary>
    /// Saves a file to storage and returns the file path, hash, and digital signature
    /// </summary>
    Task<(string FilePath, string FileHash, string DigitalSignature, long FileSize)> SaveFileAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a file stream from storage
    /// </summary>
    Task<Stream?> GetFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from storage
    /// </summary>
    Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a file exists in storage
    /// </summary>
    Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies file integrity using SHA256 hash
    /// </summary>
    Task<bool> VerifyFileHashAsync(string filePath, string expectedHash, CancellationToken cancellationToken = default);
}
