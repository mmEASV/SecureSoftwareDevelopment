using Microsoft.EntityFrameworkCore;
using Admin.Api.Infrastructure.Persistence;
using Admin.Shared.Enums;
using Admin.Shared.Models;

namespace Admin.Api.Infrastructure.Initialization;

public static class DataSeeder
{
    public static async Task SeedDataAsync(UpdateServiceDbContext context)
    {
        // Check if data already exists
        if (await context.Updates.AnyAsync())
        {
            return; // Data already seeded
        }

        // Create sample updates
        var update1 = new Update
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Version = "1.0.0",
            Title = "Initial Release",
            Description = "First stable release with core functionality",
            ChangeLog = "- Added device management\n- Implemented update system\n- Basic security features",
            SecurityFixes = new List<string>(),
            FilePath = "updates/v1.0.0.bin",
            FileHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            FileSize = 10485760, // 10 MB
            UpdateType = UpdateType.Feature,
            Severity = UpdateSeverity.Low,
            IsSecurityUpdate = false,
            TargetDeviceTypes = new List<string> { "A100", "A200" },
            CreatedAt = DateTime.UtcNow.AddMonths(-3),
            CreatedBy = Guid.Empty,
            IsActive = true
        };

        var update2 = new Update
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Version = "1.1.0",
            Title = "Security Patch - Critical CVE Fix",
            Description = "Critical security update addressing multiple vulnerabilities",
            ChangeLog = "- Fixed authentication bypass vulnerability\n- Patched SQL injection vulnerability\n- Updated encryption algorithms",
            SecurityFixes = new List<string> { "CVE-2024-0001", "CVE-2024-0002" },
            FilePath = "updates/v1.1.0.bin",
            FileHash = "5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8",
            FileSize = 12582912, // 12 MB
            UpdateType = UpdateType.Security,
            Severity = UpdateSeverity.Critical,
            IsSecurityUpdate = true,
            TargetDeviceTypes = new List<string> { "A100", "A200", "A300" },
            CreatedAt = DateTime.UtcNow.AddMonths(-1),
            CreatedBy = Guid.Empty,
            IsActive = true
        };

        var update3 = new Update
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Version = "1.2.0",
            Title = "Performance Improvements",
            Description = "Major performance optimizations and bug fixes",
            ChangeLog = "- Improved boot time by 40%\n- Fixed memory leak in logging system\n- Enhanced network stability",
            SecurityFixes = new List<string>(),
            FilePath = "updates/v1.2.0.bin",
            FileHash = "b3f9c4b9e9e1b9c0a8d6f3e2c5a7d9f1e3b5c7a9d1f3e5b7c9d1f3e5b7c9d1f3",
            FileSize = 15728640, // 15 MB
            UpdateType = UpdateType.BugFix,
            Severity = UpdateSeverity.Medium,
            IsSecurityUpdate = false,
            TargetDeviceTypes = new List<string> { "A100", "A200", "A300" },
            CreatedAt = DateTime.UtcNow.AddDays(-7),
            CreatedBy = Guid.Empty,
            IsActive = true
        };

        await context.Updates.AddRangeAsync(update1, update2, update3);
        await context.SaveChangesAsync();

        // Create releases for updates
        var release1 = new Release
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            UpdateId = update1.Id,
            ReleaseDate = DateTime.UtcNow.AddMonths(-3),
            IsActive = true,
            IsMandatory = false,
            MinimumVersion = null,
            MaxPostponeDays = 30,
            ReleaseNotes = "Initial stable release. Recommended for all new installations.",
            CreatedAt = DateTime.UtcNow.AddMonths(-3),
            CreatedBy = Guid.Empty
        };

        var release2 = new Release
        {
            Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
            UpdateId = update2.Id,
            ReleaseDate = DateTime.UtcNow.AddMonths(-1),
            IsActive = true,
            IsMandatory = true,
            MinimumVersion = "1.0.0",
            MaxPostponeDays = 7,
            ReleaseNotes = "CRITICAL SECURITY UPDATE - Must be installed within 7 days. Fixes authentication bypass and SQL injection vulnerabilities.",
            CreatedAt = DateTime.UtcNow.AddMonths(-1),
            CreatedBy = Guid.Empty
        };

        var release3 = new Release
        {
            Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
            UpdateId = update3.Id,
            ReleaseDate = DateTime.UtcNow.AddDays(-7),
            IsActive = true,
            IsMandatory = false,
            MinimumVersion = "1.1.0",
            MaxPostponeDays = 14,
            ReleaseNotes = "Performance improvements and bug fixes. Recommended for devices experiencing slow boot times.",
            CreatedAt = DateTime.UtcNow.AddDays(-7),
            CreatedBy = Guid.Empty
        };

        await context.Releases.AddRangeAsync(release1, release2, release3);
        await context.SaveChangesAsync();

        // Create sample devices
        var tenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        var device1 = new Device
        {
            Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
            DeviceIdentifier = "DEV-A100-001",
            DeviceName = "Factory Floor Machine #1",
            DeviceType = "A100",
            CurrentVersion = "1.0.0",
            ApiKey = "dev1-api-key-12345678-secure-token",
            TenantId = tenantId,
            AutomaticUpdates = true,
            SkipNonSecurityUpdates = false,
            UpdateSchedule = "0 2 * * *", // 2 AM daily
            MaintenanceWindowStart = "02:00",
            MaintenanceWindowEnd = "06:00",
            RegisteredAt = DateTime.UtcNow.AddMonths(-3),
            LastSeenAt = DateTime.UtcNow.AddHours(-2),
            IsActive = true
        };

        var device2 = new Device
        {
            Id = Guid.Parse("88888888-8888-8888-8888-888888888888"),
            DeviceIdentifier = "DEV-A200-042",
            DeviceName = "Quality Control Station",
            DeviceType = "A200",
            CurrentVersion = "1.1.0",
            ApiKey = "dev2-api-key-87654321-secure-token",
            TenantId = tenantId,
            AutomaticUpdates = true,
            SkipNonSecurityUpdates = true,
            UpdateSchedule = "0 3 * * 0", // 3 AM on Sundays
            MaintenanceWindowStart = "03:00",
            MaintenanceWindowEnd = "05:00",
            RegisteredAt = DateTime.UtcNow.AddMonths(-2),
            LastSeenAt = DateTime.UtcNow.AddMinutes(-30),
            IsActive = true
        };

        var device3 = new Device
        {
            Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
            DeviceIdentifier = "DEV-A300-123",
            DeviceName = "Packaging Line Controller",
            DeviceType = "A300",
            CurrentVersion = "1.0.0",
            ApiKey = "dev3-api-key-abcdef12-secure-token",
            TenantId = tenantId,
            AutomaticUpdates = false, // Manual updates only
            SkipNonSecurityUpdates = false,
            UpdateSchedule = null,
            MaintenanceWindowStart = null,
            MaintenanceWindowEnd = null,
            RegisteredAt = DateTime.UtcNow.AddMonths(-1),
            LastSeenAt = DateTime.UtcNow.AddDays(-5),
            IsActive = true
        };

        await context.Devices.AddRangeAsync(device1, device2, device3);
        await context.SaveChangesAsync();

        // Create sample deployments
        var deployment1 = new Deployment
        {
            Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            DeviceId = device1.Id,
            ReleaseId = release2.Id,
            ScheduledAt = DateTime.UtcNow.AddDays(-14),
            Status = DeploymentStatus.Completed,
            DownloadProgress = 100,
            InstallProgress = 100,
            StartedAt = DateTime.UtcNow.AddDays(-14).AddHours(2),
            CompletedAt = DateTime.UtcNow.AddDays(-14).AddHours(2).AddMinutes(15),
            PostponeCount = 0,
            RetryCount = 0,
            ErrorMessage = null
        };

        var deployment2 = new Deployment
        {
            Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
            DeviceId = device2.Id,
            ReleaseId = release2.Id,
            ScheduledAt = DateTime.UtcNow.AddDays(-10),
            Status = DeploymentStatus.Completed,
            DownloadProgress = 100,
            InstallProgress = 100,
            StartedAt = DateTime.UtcNow.AddDays(-10).AddHours(3),
            CompletedAt = DateTime.UtcNow.AddDays(-10).AddHours(3).AddMinutes(18),
            PostponeCount = 1,
            PostponeReason = "Scheduled maintenance window",
            RetryCount = 0,
            ErrorMessage = null
        };

        var deployment3 = new Deployment
        {
            Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
            DeviceId = device1.Id,
            ReleaseId = release3.Id,
            ScheduledAt = DateTime.UtcNow.AddDays(-3),
            Status = DeploymentStatus.Pending,
            DownloadProgress = 0,
            InstallProgress = 0,
            StartedAt = null,
            CompletedAt = null,
            PostponeCount = 0,
            RetryCount = 0,
            ErrorMessage = null
        };

        var deployment4 = new Deployment
        {
            Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            DeviceId = device3.Id,
            ReleaseId = release2.Id,
            ScheduledAt = DateTime.UtcNow.AddDays(-5),
            Status = DeploymentStatus.Failed,
            DownloadProgress = 100,
            InstallProgress = 45,
            StartedAt = DateTime.UtcNow.AddDays(-5),
            CompletedAt = null,
            PostponeCount = 0,
            RetryCount = 2,
            ErrorMessage = "Installation failed: Checksum mismatch during file extraction"
        };

        await context.Deployments.AddRangeAsync(deployment1, deployment2, deployment3, deployment4);
        await context.SaveChangesAsync();
    }
}
