using Template.Shared.Models;
using Template.Shared.Models.Identity;
using Template.Shared.Models.Interfaces;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Template.Api.Domain.Common;
using Template.Api.Infrastructure.Persistence.Conventions;
using Template.Api.Infrastructure.Persistence.Interceptors;

namespace Template.Api.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    private readonly Guid? _currentTenantId;
    private readonly bool _isAdmin;
    private readonly TimeProvider _now;

    public virtual DbSet<Tenant> Tenants { get; set; } = default!;
    // Models

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(new SoftDeleteInterceptor());
        base.OnConfiguring(optionsBuilder);
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, CurrentContext currentContext, TimeProvider now)
        : base(options)
    {
        _currentTenantId = currentContext.TenantId;
        _isAdmin = currentContext.IsAdmin();
        _now = now;

        // Subscribe to change tracker events
        ChangeTracker.Tracked += OnEntityTracked;
        ChangeTracker.StateChanged += OnEntityStateChanged;
    }

    // For testing purposes
    public ApplicationDbContext()
    {
        _now = TimeProvider.System;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Query filters
        builder.Model.GetEntityTypes()
            .ToList()
            .ForEach(entityType =>
            {
                if (typeof(IOwned).IsAssignableFrom(entityType.ClrType) &&
                    entityType.ClrType != typeof(ApplicationUser))
                {
                    builder.Entity(entityType.ClrType)
                        .AddQueryFilter<IOwned>(e => e.OwnerId == _currentTenantId || _isAdmin);
                }
                if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
                {
                    builder.Entity(entityType.ClrType)
                        .AddQueryFilter<ISoftDeletable>(e => e.DeletedAt == null);
                }
            });

        base.OnModelCreating(builder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Conventions.Add(_ => new GuidV7Convention());
    }

    void OnEntityTracked(object? sender, EntityTrackedEventArgs e)
    {
        if (e.Entry.State is EntityState.Added && e.Entry.Entity is ITrackable trackableEntity)
        {
            trackableEntity.CreatedAt = _now.GetUtcNow();
            trackableEntity.UpdatedAt = _now.GetUtcNow();
        }

        if (e.Entry.State is EntityState.Added && e.Entry.Entity is IOwned ownedEntity)
        {
            if (ownedEntity.OwnerId == default)
            {
                ownedEntity.OwnerId = _currentTenantId ?? throw new InvalidOperationException("TenantId is null");
            }
        }
    }

    void OnEntityStateChanged(object? sender, EntityStateChangedEventArgs e)
    {
        if (e.NewState is EntityState.Modified && e.Entry.Entity is ITrackable entity)
        {
            entity.UpdatedAt = _now.GetUtcNow();
        }
    }
}
