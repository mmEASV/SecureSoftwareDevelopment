using System.Security.Cryptography;
using Admin.Api.Infrastructure.Security;

namespace Admin.Api.Infrastructure.Storage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _storagePath;
    private readonly ILogger<LocalFileStorageService> _logger;
    private readonly IDigitalSignatureService _signatureService;

    public LocalFileStorageService(
        IConfiguration configuration,
        ILogger<LocalFileStorageService> logger,
        IDigitalSignatureService signatureService)
    {
        _logger = logger;
        _signatureService = signatureService;
        _storagePath = configuration["FileStorage:Path"] ?? Path.Combine(Directory.GetCurrentDirectory(), "updates");

        // Ensure storage directory exists
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
            _logger.LogInformation("Created storage directory at {Path}", _storagePath);
        }
    }

    public async Task<(string FilePath, string FileHash, string DigitalSignature, long FileSize)> SaveFileAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Generate unique file name to avoid conflicts
            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
            var fullPath = Path.Combine(_storagePath, uniqueFileName);

            // Save file and compute hash simultaneously
            using var sha256 = SHA256.Create();
            using var fileStreamOut = new FileStream(fullPath, FileMode.Create, FileAccess.Write);

            // Use CryptoStream to compute hash while writing
            using var cryptoStream = new CryptoStream(fileStreamOut, sha256, CryptoStreamMode.Write);

            await fileStream.CopyToAsync(cryptoStream, cancellationToken);
            cryptoStream.FlushFinalBlock();

            var hash = Convert.ToHexString(sha256.Hash!).ToLowerInvariant();
            var fileInfo = new FileInfo(fullPath);

            // Create digital signature of the hash
            var signature = await _signatureService.SignHashAsync(hash, cancellationToken);

            var hashPreview = hash.Length > 16 ? hash[..16] + "..." : hash;
            var sigPreview = signature.Length > 32 ? signature[..32] + "..." : signature;
            _logger.LogInformation("Saved file {FileName} to {Path} with hash {Hash} and signature {SigPreview}",
                fileName, uniqueFileName, hashPreview, sigPreview);

            return (uniqueFileName, hash, signature, fileInfo.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving file {FileName}", fileName);
            throw;
        }
    }

    public async Task<Stream?> GetFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(_storagePath, filePath);

            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("File not found: {Path}", filePath);
                return null;
            }

            var memoryStream = new MemoryStream();
            using var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            await fileStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            return memoryStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving file {Path}", filePath);
            throw;
        }
    }

    public Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(_storagePath, filePath);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Deleted file {Path}", filePath);
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {Path}", filePath);
            throw;
        }
    }

    public Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_storagePath, filePath);
        return Task.FromResult(File.Exists(fullPath));
    }

    public async Task<bool> VerifyFileHashAsync(string filePath, string expectedHash, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(_storagePath, filePath);

            if (!File.Exists(fullPath))
            {
                return false;
            }

            using var sha256 = SHA256.Create();
            using var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);

            var hash = await sha256.ComputeHashAsync(fileStream, cancellationToken);
            var hashString = Convert.ToHexString(hash).ToLowerInvariant();

            return hashString.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying file hash for {Path}", filePath);
            return false;
        }
    }
}
