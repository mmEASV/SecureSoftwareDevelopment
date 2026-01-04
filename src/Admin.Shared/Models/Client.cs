using System.ComponentModel.DataAnnotations;

namespace Admin.Shared.Models;

/// <summary>
/// Represents a customer/client that receives updates via webhook
/// </summary>
public class Client
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Webhook URL where this client receives update notifications
    /// Example: https://customer-tunnel.trycloudflare.com/api/webhooks/release-notification
    /// </summary>
    [Required]
    [StringLength(500)]
    [Url]
    public string WebhookUrl { get; set; } = string.Empty;

    /// <summary>
    /// Shared secret for HMAC signature verification
    /// Used to ensure webhook authenticity
    /// </summary>
    [Required]
    [StringLength(256)]
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>
    /// Whether this client is active and should receive webhooks
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Last time a webhook was successfully delivered to this client
    /// </summary>
    public DateTime? LastWebhookSuccess { get; set; }

    /// <summary>
    /// Last time a webhook delivery failed for this client
    /// </summary>
    public DateTime? LastWebhookFailure { get; set; }

    /// <summary>
    /// Number of consecutive webhook failures
    /// Used to temporarily disable webhooks if client is unreachable
    /// </summary>
    public int ConsecutiveFailures { get; set; }

    /// <summary>
    /// Contact email for this client
    /// </summary>
    [StringLength(200)]
    [EmailAddress]
    public string? ContactEmail { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }
}
