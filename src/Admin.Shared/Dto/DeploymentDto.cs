using Admin.Shared.Enums;

namespace Admin.Shared.Dto;

public record DeploymentDto
{
    public Guid Id { get; init; }
    public Guid ReleaseId { get; init; }
    public ReleaseDto? Release { get; init; }
    public Guid DeviceId { get; init; }
    public DeviceDto? Device { get; init; }
    public DateTime? ScheduledAt { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DeploymentStatus Status { get; init; }
    public string? ErrorMessage { get; init; }
    public int RetryCount { get; init; }
    public string? PostponeReason { get; init; }
    public int PostponeCount { get; init; }
    public DateTime? LastPostponedAt { get; init; }
    public int? DownloadProgress { get; init; }
    public int? InstallProgress { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record ScheduleDeploymentDto
{
    public Guid ReleaseId { get; init; }
    public List<Guid> DeviceIds { get; init; } = new();
    public DateTime? ScheduledAt { get; init; }
}

public record PostponeDeploymentDto
{
    public string? Reason { get; init; }
    public DateTime PostponeUntil { get; init; }
}

public record UpdateDeploymentStatusDto
{
    public DeploymentStatus Status { get; init; }
    public string? ErrorMessage { get; init; }
    public int? DownloadProgress { get; init; }
    public int? InstallProgress { get; init; }
}
