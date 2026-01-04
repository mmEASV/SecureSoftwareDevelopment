using System.ComponentModel.DataAnnotations;
using Admin.Shared.Enums;

namespace Admin.Shared.Models;

/// <summary>
/// Represents the deployment of a release to a specific device
/// </summary>
public class Deployment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ReleaseId { get; set; }

    [Required]
    public Guid DeviceId { get; set; }

    /// <summary>
    /// When the deployment is scheduled to execute
    /// </summary>
    public DateTime? ScheduledAt { get; set; }

    /// <summary>
    /// When the deployment actually started
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When the deployment completed (success or failure)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    public DeploymentStatus Status { get; set; } = DeploymentStatus.Pending;

    [StringLength(1000)]
    public string? ErrorMessage { get; set; }

    public int RetryCount { get; set; }

    public DateTime? LastRetryAt { get; set; }

    /// <summary>
    /// Reason for postponing (if applicable)
    /// </summary>
    [StringLength(500)]
    public string? PostponeReason { get; set; }

    /// <summary>
    /// How many times has this deployment been postponed
    /// </summary>
    public int PostponeCount { get; set; }

    /// <summary>
    /// When was it last postponed
    /// </summary>
    public DateTime? LastPostponedAt { get; set; }

    /// <summary>
    /// Download progress percentage (0-100)
    /// </summary>
    public int? DownloadProgress { get; set; }

    /// <summary>
    /// Installation progress percentage (0-100)
    /// </summary>
    public int? InstallProgress { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid? CreatedBy { get; set; }

    // Navigation properties
    public virtual Release Release { get; set; } = null!;
    public virtual Device Device { get; set; } = null!;
}
