using Microsoft.EntityFrameworkCore;
using ClientPortal.Api.Infrastructure.Persistence;
using Admin.Shared.Enums;
using Admin.Shared.Models;

namespace ClientPortal.Api.Infrastructure.Initialization;

public static class DataSeeder
{
    public static async Task SeedDataAsync(UpdateServiceDbContext context)
    {
        // Check if data already exists
        if (await context.Devices.AnyAsync())
        {
            return; // Data already seeded
        }

        // Create sample devices for testing
        var tenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        var device1 = new Device
        {
            Id = Guid.Parse("d1111111-1111-1111-1111-111111111111"),
            DeviceIdentifier = "CLIENT-DEV-001",
            DeviceName = "Customer Device 1",
            DeviceType = "PackagingMachine",
            CurrentVersion = "1.0.0",
            ApiKey = "client-dev1-api-key-secure-token-123",
            TenantId = tenantId,
            AutomaticUpdates = true,
            SkipNonSecurityUpdates = false,
            UpdateSchedule = "0 2 * * *", // 2 AM daily
            MaintenanceWindowStart = "02:00",
            MaintenanceWindowEnd = "06:00",
            RegisteredAt = DateTime.UtcNow.AddMonths(-2),
            LastSeenAt = DateTime.UtcNow.AddHours(-1),
            IsActive = true
        };

        var device2 = new Device
        {
            Id = Guid.Parse("d2222222-2222-2222-2222-222222222222"),
            DeviceIdentifier = "CLIENT-DEV-002",
            DeviceName = "Customer Device 2",
            DeviceType = "QualityControlStation",
            CurrentVersion = "1.1.0",
            ApiKey = "client-dev2-api-key-secure-token-456",
            TenantId = tenantId,
            AutomaticUpdates = true,
            SkipNonSecurityUpdates = true,
            UpdateSchedule = "0 3 * * 0", // 3 AM on Sundays
            MaintenanceWindowStart = "03:00",
            MaintenanceWindowEnd = "05:00",
            RegisteredAt = DateTime.UtcNow.AddMonths(-1),
            LastSeenAt = DateTime.UtcNow.AddMinutes(-15),
            IsActive = true
        };

        var device3 = new Device
        {
            Id = Guid.Parse("d3333333-3333-3333-3333-333333333333"),
            DeviceIdentifier = "CLIENT-DEV-003",
            DeviceName = "Customer Device 3",
            DeviceType = "ProductionController",
            CurrentVersion = "1.0.0",
            ApiKey = "client-dev3-api-key-secure-token-789",
            TenantId = tenantId,
            AutomaticUpdates = false, // Manual updates only
            SkipNonSecurityUpdates = false,
            UpdateSchedule = null,
            MaintenanceWindowStart = null,
            MaintenanceWindowEnd = null,
            RegisteredAt = DateTime.UtcNow.AddDays(-30),
            LastSeenAt = DateTime.UtcNow.AddDays(-2),
            IsActive = true
        };

        await context.Devices.AddRangeAsync(device1, device2, device3);
        await context.SaveChangesAsync();

        // Note: Releases will be synced from Admin.Api via ReleaseSyncService
        // Note: Deployments can be created through the ClientPortal.Web UI or API
    }
}
