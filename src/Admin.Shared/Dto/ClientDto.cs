using System.ComponentModel.DataAnnotations;

namespace Admin.Shared.Dto;

public record ClientDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string WebhookUrl { get; init; } = string.Empty;
    public string WebhookSecret { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime? LastWebhookSuccess { get; init; }
    public DateTime? LastWebhookFailure { get; init; }
    public int ConsecutiveFailures { get; init; }
    public string? ContactEmail { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class CreateClientDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [StringLength(500)]
    [Url]
    public string WebhookUrl { get; set; } = string.Empty;

    [StringLength(200)]
    [EmailAddress]
    public string? ContactEmail { get; set; }
}

public class UpdateClientDto
{
    [StringLength(200)]
    public string? Name { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(500)]
    [Url]
    public string? WebhookUrl { get; set; }

    public bool? IsActive { get; set; }

    [StringLength(200)]
    [EmailAddress]
    public string? ContactEmail { get; set; }
}
