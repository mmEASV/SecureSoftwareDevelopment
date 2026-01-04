using Microsoft.EntityFrameworkCore;
using Admin.Api.Domain.Interfaces;
using Admin.Api.Endpoints.Deployments;
using Admin.Api.Endpoints.Devices;
using Admin.Api.Endpoints.Releases;
using Admin.Api.Endpoints.Sync;
using Admin.Api.Endpoints.Updates;
using Admin.Api.Infrastructure.Initialization;
using Admin.Api.Infrastructure.Persistence;
using Admin.Api.Infrastructure.Repositories;
using Admin.Api.Infrastructure.Storage;
using Admin.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults (Aspire)
builder.AddServiceDefaults();

// Add database
builder.AddNpgsqlDbContext<UpdateServiceDbContext>("UpdateServiceDb");

// Add repositories
builder.Services.AddScoped<IUpdateRepository, UpdateRepository>();
builder.Services.AddScoped<IReleaseRepository, ReleaseRepository>();
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<IDeploymentRepository, DeploymentRepository>();

// Add security services
builder.Services.AddSingleton<Admin.Api.Infrastructure.Security.IDigitalSignatureService, Admin.Api.Infrastructure.Security.RsaDigitalSignatureService>();

// Add storage service
builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();

// Add webhook notification service
builder.Services.AddScoped<IWebhookNotificationService, WebhookNotificationService>();

// Add HttpClient for webhooks
builder.Services.AddHttpClient("WebhookClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();
    if (builder.Environment.IsDevelopment())
    {
        handler.ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    }
    return handler;
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(name: "database");

// Add OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Vendor Update Service API",
        Version = "v1",
        Description = "API for managing device updates and deployments (CRA Compliance)"
    });
});

var app = builder.Build();

// Configure middleware
app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Update Service API v1");
    });
}

app.UseCors();

// Map endpoints
var api = app.MapGroup("/api");

api.MapGroup("/updates").MapUpdateEndpoints();
api.MapGroup("/releases").MapReleaseEndpoints();
api.MapGroup("/devices").MapDeviceEndpoints();
api.MapGroup("/deployments").MapDeploymentEndpoints();
api.MapSyncEndpoints(); // Sync endpoints for client to pull through Cloudflare tunnel

// Health check
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "Admin.Api" }))
    .WithName("HealthCheck");

// Map default endpoints
app.MapDefaultEndpoints();

// Run database migrations and seed data in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<UpdateServiceDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Running database migrations...");
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Database migrations completed successfully");

        logger.LogInformation("Seeding initial data...");
        await DataSeeder.SeedDataAsync(dbContext);
        logger.LogInformation("Data seeding completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating or seeding the database");
        // Don't throw - let the app start and report unhealthy via health checks
    }
}

app.Run();

// Make Program class accessible for testing
public partial class Program { }
