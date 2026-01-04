using System.ComponentModel.DataAnnotations;

namespace Admin.Shared.Models;

/// <summary>
/// Represents a release of an update to customers
/// </summary>
public class Release
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UpdateId { get; set; }

    public DateTime ReleaseDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether this release is currently available for deployment
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Mandatory updates must be installed (for critical security fixes)
    /// </summary>
    public bool IsMandatory { get; set; }

    /// <summary>
    /// Minimum version required before this update can be applied
    /// </summary>
    [StringLength(50)]
    public string? MinimumVersion { get; set; }

    /// <summary>
    /// Maximum days a mandatory security update can be postponed (CRA compliance)
    /// </summary>
    public int MaxPostponeDays { get; set; } = 7;

    [StringLength(2000)]
    public string? ReleaseNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid CreatedBy { get; set; }

    // Navigation properties
    public virtual Update Update { get; set; } = null!;
    public virtual ICollection<Deployment> Deployments { get; set; } = new List<Deployment>();
}
