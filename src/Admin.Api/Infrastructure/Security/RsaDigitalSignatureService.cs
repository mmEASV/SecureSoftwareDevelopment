using System.Security.Cryptography;
using System.Text;

namespace Admin.Api.Infrastructure.Security;

/// <summary>
/// RSA-4096 digital signature service for update file authentication
/// Implements non-repudiation and authenticity verification
/// </summary>
public class RsaDigitalSignatureService : IDigitalSignatureService, IDisposable
{
    private readonly RSA _rsa;
    private readonly ILogger<RsaDigitalSignatureService> _logger;
    private readonly string _publicKeyPem;
    private readonly string _publicKeyXml;

    public RsaDigitalSignatureService(IConfiguration configuration, ILogger<RsaDigitalSignatureService> logger)
    {
        _logger = logger;
        _rsa = RSA.Create(4096); // RSA-4096 for strong security

        var privateKeyPath = configuration["DigitalSignature:PrivateKeyPath"];
        var publicKeyPath = configuration["DigitalSignature:PublicKeyPath"];

        if (!string.IsNullOrEmpty(privateKeyPath) && File.Exists(privateKeyPath))
        {
            // Load existing private key
            var privateKeyPem = File.ReadAllText(privateKeyPath);
            _rsa.ImportFromPem(privateKeyPem);
            _logger.LogInformation("Loaded existing RSA private key from {Path}", privateKeyPath);
        }
        else if (!string.IsNullOrEmpty(publicKeyPath) && File.Exists(publicKeyPath))
        {
            // Load public key only (for verification-only scenarios)
            var publicKeyPem = File.ReadAllText(publicKeyPath);
            _rsa.ImportFromPem(publicKeyPem);
            _logger.LogInformation("Loaded existing RSA public key from {Path}", publicKeyPath);
        }
        else
        {
            // Generate new key pair
            _logger.LogWarning("No existing keys found. Generating new RSA-4096 key pair.");
            _logger.LogWarning("IMPORTANT: In production, keys should be pre-generated and securely stored!");

            // Create keys directory if it doesn't exist
            var keysDir = Path.Combine(Directory.GetCurrentDirectory(), "keys");
            Directory.CreateDirectory(keysDir);

            privateKeyPath = Path.Combine(keysDir, "vendor-private-key.pem");
            publicKeyPath = Path.Combine(keysDir, "vendor-public-key.pem");

            // Export and save keys
            var newPrivateKey = _rsa.ExportRSAPrivateKeyPem();
            var newPublicKey = _rsa.ExportRSAPublicKeyPem();

            File.WriteAllText(privateKeyPath, newPrivateKey);
            File.WriteAllText(publicKeyPath, newPublicKey);

            // Set restrictive permissions on private key (Unix only)
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(privateKeyPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }

            _logger.LogInformation("Generated new RSA key pair:");
            _logger.LogInformation("  Private Key: {PrivatePath} (KEEP SECURE!)", privateKeyPath);
            _logger.LogInformation("  Public Key: {PublicPath}", publicKeyPath);
        }

        // Cache public key formats
        _publicKeyPem = _rsa.ExportRSAPublicKeyPem();
        _publicKeyXml = _rsa.ToXmlString(false); // false = public key only
    }

    public Task<string> SignHashAsync(string fileHash, CancellationToken cancellationToken = default)
    {
        try
        {
            // Convert hex hash to bytes
            var hashBytes = Convert.FromHexString(fileHash);

            // Sign the hash using RSA with SHA256
            var signature = _rsa.SignHash(hashBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            // Return base64-encoded signature
            var signatureBase64 = Convert.ToBase64String(signature);

            _logger.LogInformation("Signed file hash {Hash} (signature length: {Length} bytes)",
                fileHash[..16] + "...", signature.Length);

            return Task.FromResult(signatureBase64);
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Failed to sign hash {Hash}. Private key may not be loaded.", fileHash[..16]);
            throw new InvalidOperationException("Digital signature creation failed. Private key not available.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error signing hash");
            throw;
        }
    }

    public Task<bool> VerifySignatureAsync(string fileHash, string signature, CancellationToken cancellationToken = default)
    {
        try
        {
            // Convert inputs
            var hashBytes = Convert.FromHexString(fileHash);
            var signatureBytes = Convert.FromBase64String(signature);

            // Verify signature using public key
            var isValid = _rsa.VerifyHash(hashBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            if (isValid)
            {
                _logger.LogInformation("Signature verification PASSED for hash {Hash}", fileHash[..16] + "...");
            }
            else
            {
                _logger.LogWarning("Signature verification FAILED for hash {Hash}", fileHash[..16] + "...");
            }

            return Task.FromResult(isValid);
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Invalid signature or hash format");
            return Task.FromResult(false);
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Cryptographic error during signature verification");
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error verifying signature");
            return Task.FromResult(false);
        }
    }

    public string GetPublicKeyPem()
    {
        return _publicKeyPem;
    }

    public string GetPublicKeyXml()
    {
        return _publicKeyXml;
    }

    public void Dispose()
    {
        _rsa?.Dispose();
    }
}
