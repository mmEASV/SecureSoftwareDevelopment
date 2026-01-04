using Admin.Shared.Enums;

namespace Admin.Shared.Dto;

public record UpdateDto
{
    public Guid Id { get; init; }
    public string Version { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ChangeLog { get; init; }
    public List<string> SecurityFixes { get; init; } = new();
    public string FileHash { get; init; } = string.Empty;
    public string DigitalSignature { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public UpdateType UpdateType { get; init; }
    public UpdateSeverity Severity { get; init; }
    public bool IsSecurityUpdate { get; init; }
    public List<string> TargetDeviceTypes { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public bool IsActive { get; init; }
}

public class CreateUpdateDto
{
    public string Version { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ChangeLog { get; set; }
    public List<string> SecurityFixes { get; set; } = new();
    public UpdateType UpdateType { get; set; }
    public UpdateSeverity Severity { get; set; }
    public bool IsSecurityUpdate { get; set; }
    public List<string> TargetDeviceTypes { get; set; } = new();
}

public record UpdateUpdateDto
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? ChangeLog { get; init; }
    public List<string>? SecurityFixes { get; init; }
    public UpdateType? UpdateType { get; init; }
    public UpdateSeverity? Severity { get; init; }
    public bool? IsSecurityUpdate { get; init; }
    public bool? IsActive { get; init; }
}
