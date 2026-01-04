namespace Admin.Api.Infrastructure.Security;

/// <summary>
/// Service for creating and verifying digital signatures for update files
/// Uses RSA-4096 for strong cryptographic security
/// </summary>
public interface IDigitalSignatureService
{
    /// <summary>
    /// Signs a file hash with the private key
    /// </summary>
    /// <param name="fileHash">SHA256 hash of the file (hex string)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Base64-encoded digital signature</returns>
    Task<string> SignHashAsync(string fileHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a signature against a file hash using the public key
    /// </summary>
    /// <param name="fileHash">SHA256 hash of the file (hex string)</param>
    /// <param name="signature">Base64-encoded digital signature</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if signature is valid, false otherwise</returns>
    Task<bool> VerifySignatureAsync(string fileHash, string signature, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the public key in PEM format for distribution to devices
    /// </summary>
    /// <returns>Public key in PEM format</returns>
    string GetPublicKeyPem();

    /// <summary>
    /// Exports the public key in XML format
    /// </summary>
    /// <returns>Public key in XML format</returns>
    string GetPublicKeyXml();
}
