using System.ComponentModel.DataAnnotations;
using Admin.Shared.Enums;

namespace Admin.Shared.Models;

/// <summary>
/// Represents a software update package created by Vendor
/// </summary>
public class Update
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(50)]
    public string Version { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(5000)]
    public string? ChangeLog { get; set; }

    /// <summary>
    /// List of security vulnerabilities fixed (CRA requirement)
    /// </summary>
    public List<string> SecurityFixes { get; set; } = new();

    [Required]
    [StringLength(500)]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// SHA256 hash for file integrity verification
    /// </summary>
    [Required]
    [StringLength(64)]
    public string FileHash { get; set; } = string.Empty;

    /// <summary>
    /// RSA-4096 digital signature of the file hash for authenticity verification
    /// Base64-encoded signature created with Vendor's private key
    /// </summary>
    [Required]
    [StringLength(1024)] // RSA-4096 signature is 512 bytes, base64 is ~700 chars
    public string DigitalSignature { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public UpdateType UpdateType { get; set; }

    public UpdateSeverity Severity { get; set; }

    /// <summary>
    /// Indicates if this update addresses security vulnerabilities (CRA requirement)
    /// </summary>
    public bool IsSecurityUpdate { get; set; }

    /// <summary>
    /// Target device types for this update (e.g., "A100", "B200")
    /// </summary>
    public List<string> TargetDeviceTypes { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<Release> Releases { get; set; } = new List<Release>();
}
