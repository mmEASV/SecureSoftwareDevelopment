using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Admin.Shared.Dto;

var builder = Host.CreateApplicationBuilder(args);

// Configure configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Add HTTP client for ClientPortal.Api (releases list)
builder.Services.AddHttpClient("ClientPortalApi", (sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var baseUrl = config["UpdateAgent:ClientPortalApiUrl"] ?? throw new Exception("ClientPortalApiUrl not configured");
    var apiKey = config["UpdateAgent:DeviceApiKey"] ?? throw new Exception("DeviceApiKey not configured");

    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
    client.Timeout = TimeSpan.FromMinutes(1);
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();
    if (builder.Environment.IsDevelopment())
    {
        handler.ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    }
    return handler;
});

// Add HTTP client for Admin.Api (file downloads)
builder.Services.AddHttpClient("AdminApi", (sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var baseUrl = config["UpdateAgent:AdminApiUrl"] ?? throw new Exception("AdminApiUrl not configured");

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromMinutes(10); // Long timeout for large file downloads
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();
    if (builder.Environment.IsDevelopment())
    {
        handler.ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    }
    return handler;
});

// Add background service
builder.Services.AddHostedService<UpdateCheckerService>();

var host = builder.Build();
await host.RunAsync();

// Background service that checks for updates
public class UpdateCheckerService : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<UpdateCheckerService> _logger;

    public UpdateCheckerService(
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<UpdateCheckerService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = _config.GetValue<int>("UpdateAgent:CheckIntervalMinutes");
        var interval = TimeSpan.FromMinutes(intervalMinutes);

        _logger.LogInformation("Device Agent started. Check interval: {Interval} minutes", intervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Checking for updates...");
                await CheckForUpdatesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for updates");
            }

            _logger.LogInformation("Next check in {Interval} minutes", intervalMinutes);
            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task CheckForUpdatesAsync(CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("ClientPortalApi");

        // Get active releases from ClientPortal.Api
        var releases = await client.GetFromJsonAsync<List<ReleaseDto>>("releases/active", cancellationToken);

        if (releases == null || releases.Count == 0)
        {
            _logger.LogInformation("No active releases available");
            return;
        }

        _logger.LogInformation("Found {Count} active releases", releases.Count);

        foreach (var release in releases)
        {
            var currentVersion = GetCurrentVersion();
            var releaseVersion = release.Update?.Version ?? "unknown";

            _logger.LogInformation("Checking release: {Version} (Current: {CurrentVersion})",
                releaseVersion, currentVersion);

            // Check if update is needed
            if (IsNewerVersion(releaseVersion, currentVersion))
            {
                _logger.LogInformation("New update available: {Version}", releaseVersion);

                if (_config.GetValue<bool>("UpdateAgent:AutoInstall"))
                {
                    await DownloadAndInstallAsync(release, cancellationToken);
                }
                else
                {
                    _logger.LogInformation("Auto-install disabled, skipping");
                }
            }
            else
            {
                _logger.LogInformation("Already on version {Version} or newer", currentVersion);
            }
        }
    }

    private async Task DownloadAndInstallAsync(ReleaseDto release, CancellationToken cancellationToken)
    {
        if (release.Update == null)
        {
            _logger.LogWarning("Release has no update attached, skipping");
            return;
        }

        var updateId = release.UpdateId;
        var version = release.Update.Version;

        _logger.LogInformation("Starting download for update {Version}...", version);

        try
        {
            var adminClient = _httpClientFactory.CreateClient("AdminApi");

            // Create download directory
            var downloadDir = _config["UpdateAgent:DownloadDirectory"] ?? "./downloads";
            Directory.CreateDirectory(downloadDir);

            var filePath = Path.Combine(downloadDir, $"update-{version}.bin");

            // Download file from Admin.Api
            _logger.LogInformation("Downloading to {FilePath}...", filePath);
            var response = await adminClient.GetAsync($"updates/{updateId}/download", cancellationToken);
            response.EnsureSuccessStatusCode();

            await using (var fileStream = File.Create(filePath))
            {
                await response.Content.CopyToAsync(fileStream, cancellationToken);
            }

            _logger.LogInformation("Download completed. Verifying integrity and authenticity...");

            // Step 1: Verify SHA256 hash (integrity check)
            var actualHash = ComputeSha256(filePath);
            var expectedHash = release.Update.FileHash;

            if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("❌ SECURITY: Hash mismatch! Expected: {Expected}, Got: {Actual}",
                    expectedHash, actualHash);
                _logger.LogError("Update rejected due to integrity check failure");
                File.Delete(filePath); // Delete corrupted file
                return;
            }

            _logger.LogInformation("✅ Integrity check passed!");

            // Step 2: Verify digital signature (authenticity check)
            var signature = release.Update.DigitalSignature;

            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogError("❌ SECURITY: No digital signature found for update {Version}", version);
                _logger.LogError("Update rejected - unsigned updates are not allowed");
                File.Delete(filePath); // Delete unsigned file
                return;
            }

            var isSignatureValid = VerifyDigitalSignature(actualHash, signature);

            if (!isSignatureValid)
            {
                _logger.LogError("❌ SECURITY: Digital signature verification FAILED for update {Version}", version);
                _logger.LogError("This update may not be from Vendor or has been tampered with!");
                _logger.LogError("Update rejected due to signature verification failure");
                File.Delete(filePath); // Delete potentially malicious file
                return;
            }

            _logger.LogInformation("✅ Digital signature verification passed!");
            _logger.LogInformation("✅ Update authenticity confirmed - signed by Vendor");

            // Install
            _logger.LogInformation("Installing update {Version}...", version);

            var installScript = _config["UpdateAgent:InstallScript"] ?? "./install.sh";

            if (File.Exists(installScript))
            {
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = installScript,
                    Arguments = filePath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = System.Diagnostics.Process.Start(processInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync(cancellationToken);

                    var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
                    var error = await process.StandardError.ReadToEndAsync(cancellationToken);

                    if (process.ExitCode == 0)
                    {
                        _logger.LogInformation("Update installed successfully!");
                        _logger.LogInformation("Install output: {Output}", output);

                        // Update current version file
                        File.WriteAllText(Path.Combine(downloadDir, "current-version.txt"), version);
                    }
                    else
                    {
                        _logger.LogError("Installation failed with exit code {ExitCode}", process.ExitCode);
                        _logger.LogError("Error: {Error}", error);
                    }
                }
            }
            else
            {
                _logger.LogWarning("Install script not found at {Path}. Skipping installation.", installScript);
                _logger.LogInformation("Update downloaded and verified, but not installed automatically.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download/install update");
        }
    }

    private string GetCurrentVersion()
    {
        try
        {
            var downloadDir = _config["UpdateAgent:DownloadDirectory"] ?? "./downloads";
            var versionFile = Path.Combine(downloadDir, "current-version.txt");

            if (File.Exists(versionFile))
            {
                return File.ReadAllText(versionFile).Trim();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read current version file");
        }

        return "1.0.0"; // Default version
    }

    private bool IsNewerVersion(string remoteVersion, string currentVersion)
    {
        try
        {
            return Version.Parse(remoteVersion) > Version.Parse(currentVersion);
        }
        catch
        {
            _logger.LogWarning("Failed to parse versions. Remote: {Remote}, Current: {Current}",
                remoteVersion, currentVersion);
            return false;
        }
    }

    private string ComputeSha256(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha256.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private bool VerifyDigitalSignature(string fileHash, string signatureBase64)
    {
        try
        {
            // Load Vendor's public key from configuration
            var publicKeyPem = _config["UpdateAgent:AgarmkowPublicKey"];

            if (string.IsNullOrEmpty(publicKeyPem))
            {
                // Try to load from file
                var publicKeyPath = _config["UpdateAgent:PublicKeyPath"] ?? "./vendor-public-key.pem";

                if (File.Exists(publicKeyPath))
                {
                    publicKeyPem = File.ReadAllText(publicKeyPath);
                    _logger.LogInformation("Loaded Vendor public key from {Path}", publicKeyPath);
                }
                else
                {
                    _logger.LogError("Vendor public key not found. Cannot verify signatures.");
                    _logger.LogError("Please configure DeviceAgent:PublicKeyPath or DeviceAgent:AgarmkowPublicKey");
                    return false;
                }
            }

            // Create RSA instance and import public key
            using var rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem);

            // Convert inputs
            var hashBytes = Convert.FromHexString(fileHash);
            var signatureBytes = Convert.FromBase64String(signatureBase64);

            // Verify signature using public key
            var isValid = rsa.VerifyHash(hashBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            return isValid;
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Cryptographic error during signature verification");
            return false;
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Invalid signature or hash format");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error verifying digital signature");
            return false;
        }
    }
}
