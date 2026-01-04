using Microsoft.EntityFrameworkCore;
using Admin.Shared.Models;

namespace Admin.Api.Infrastructure.Persistence;

public class UpdateServiceDbContext : DbContext
{
    public UpdateServiceDbContext(DbContextOptions<UpdateServiceDbContext> options)
        : base(options)
    {
    }

    public DbSet<Update> Updates => Set<Update>();
    public DbSet<Release> Releases => Set<Release>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Deployment> Deployments => Set<Deployment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UpdateServiceDbContext).Assembly);

        // Configure Update entity
        modelBuilder.Entity<Update>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Version);
            entity.Property(e => e.SecurityFixes).HasColumnType("jsonb");
            entity.Property(e => e.TargetDeviceTypes).HasColumnType("jsonb");

            entity.HasMany(e => e.Releases)
                .WithOne(e => e.Update)
                .HasForeignKey(e => e.UpdateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Release entity
        modelBuilder.Entity<Release>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UpdateId, e.IsActive });

            entity.HasMany(e => e.Deployments)
                .WithOne(e => e.Release)
                .HasForeignKey(e => e.ReleaseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Device entity
        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DeviceIdentifier).IsUnique();
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.IsActive });

            entity.HasMany(e => e.Deployments)
                .WithOne(e => e.Device)
                .HasForeignKey(e => e.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Deployment entity
        modelBuilder.Entity<Deployment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.DeviceId, e.Status });
            entity.HasIndex(e => new { e.ReleaseId, e.Status });
            entity.HasIndex(e => e.ScheduledAt);
        });
    }
}
