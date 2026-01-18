namespace Admin.Shared.Dto;

public record DeviceDto
{
    public Guid Id { get; init; }
    public string DeviceIdentifier { get; init; } = string.Empty;
    public string DeviceName { get; init; } = string.Empty;
    public string DeviceType { get; init; } = string.Empty;
    public Guid TenantId { get; init; }
    public string CurrentVersion { get; init; } = string.Empty;
    public DateTime? LastSeenAt { get; init; }
    public DateTime? LastUpdateCheck { get; init; }
    public bool IsActive { get; init; }
    public DateTime RegisteredAt { get; init; }
    public bool AutomaticUpdates { get; init; }
    public string? UpdateSchedule { get; init; }
    public bool SkipNonSecurityUpdates { get; init; }
    public bool PostponeSecurityUpdates { get; init; }
    public string? MaintenanceWindowStart { get; init; }
    public string? MaintenanceWindowEnd { get; init; }
}

public class RegisterDeviceDto
{
    public string DeviceIdentifier { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string CurrentVersion { get; set; } = "0.0.0";
}

public record UpdateDeviceDto
{
    public string? DeviceName { get; init; }
    public string? CurrentVersion { get; init; }
    public bool? IsActive { get; init; }
}

public class UpdateDeviceSettingsDto
{
    public bool? AutomaticUpdates { get; set; }
    public string? UpdateSchedule { get; set; }
    public bool? SkipNonSecurityUpdates { get; set; }
    public bool? PostponeSecurityUpdates { get; set; }
    public string? MaintenanceWindowStart { get; set; }
    public string? MaintenanceWindowEnd { get; set; }
}
