using System.ComponentModel.DataAnnotations;

namespace Admin.Shared.Models;

/// <summary>
/// Represents a physical device deployed at a customer site
/// </summary>
public class Device
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Unique identifier for the device (e.g., serial number)
    /// </summary>
    [Required]
    [StringLength(100)]
    public string DeviceIdentifier { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// Device model/type (e.g., "A100", "B200")
    /// </summary>
    [Required]
    [StringLength(50)]
    public string DeviceType { get; set; } = string.Empty;

    /// <summary>
    /// Customer/company that owns this device
    /// </summary>
    [Required]
    public Guid TenantId { get; set; }

    [Required]
    [StringLength(50)]
    public string CurrentVersion { get; set; } = "0.0.0";

    public DateTime? LastSeenAt { get; set; }

    public DateTime? LastUpdateCheck { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// API key for device authentication
    /// </summary>
    [StringLength(200)]
    public string? ApiKey { get; set; }

    // Update settings (CRA compliance)
    public bool AutomaticUpdates { get; set; } = true; // CRA: enabled by default

    [StringLength(100)]
    public string? UpdateSchedule { get; set; } // Cron expression

    public bool SkipNonSecurityUpdates { get; set; }

    public bool PostponeSecurityUpdates { get; set; } // CRA: opt-out mechanism

    /// <summary>
    /// Maintenance window start time (24-hour format, e.g., "22:00")
    /// </summary>
    [StringLength(5)]
    public string? MaintenanceWindowStart { get; set; }

    /// <summary>
    /// Maintenance window end time (24-hour format, e.g., "04:00")
    /// </summary>
    [StringLength(5)]
    public string? MaintenanceWindowEnd { get; set; }

    // Navigation properties
    public virtual ICollection<Deployment> Deployments { get; set; } = new List<Deployment>();
}
