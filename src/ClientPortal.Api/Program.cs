using Microsoft.EntityFrameworkCore;
using ClientPortal.Api.Domain.Interfaces;
using ClientPortal.Api.Endpoints.Deployments;
using ClientPortal.Api.Endpoints.Devices;
using ClientPortal.Api.Endpoints.Releases;
using ClientPortal.Api.Endpoints.Webhooks;
using ClientPortal.Api.Infrastructure.Persistence;
using ClientPortal.Api.Infrastructure.Repositories;
using ClientPortal.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults (Aspire)
builder.AddServiceDefaults();

// Add database (ClientDb - customer-side database)
builder.AddNpgsqlDbContext<UpdateServiceDbContext>("UpdateServiceDb");

// Add repositories
builder.Services.AddScoped<IReleaseRepository, ReleaseRepository>();
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<IDeploymentRepository, DeploymentRepository>();
builder.Services.AddScoped<IUpdateRepository, UpdateRepository>();

// Configure sync settings
builder.Services.Configure<SyncConfiguration>(builder.Configuration.GetSection("Sync"));

// Add HttpClient for Admin.Api (through Cloudflare tunnel)
builder.Services.AddHttpClient("AdminApi", (sp, client) =>
{
    var config = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SyncConfiguration>>().Value;
    if (!string.IsNullOrEmpty(config.AdminApiUrl))
    {
        client.BaseAddress = new Uri(config.AdminApiUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
    }
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();
    // In development, accept self-signed certificates
    if (builder.Environment.IsDevelopment())
    {
        handler.ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    }
    return handler;
});

// Add background service for syncing releases from Admin.Api
builder.Services.AddHostedService<ReleaseSyncService>();

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
        Title = "Client Portal API",
        Version = "v1",
        Description = "Customer-facing API for viewing updates and managing devices (CRA Compliance)"
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
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Client Portal API v1");
    });
}

app.UseCors();

// Map endpoints (customer-facing only)
var api = app.MapGroup("/api");

api.MapGroup("/releases").MapReleaseEndpoints();
api.MapGroup("/devices").MapDeviceEndpoints();
api.MapGroup("/deployments").MapDeploymentEndpoints();
api.MapWebhookEndpoints(); // Webhook receiver for Admin.Api notifications

// Health check
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "ClientPortal.Api" }))
    .WithName("HealthCheck");

// Map default endpoints
app.MapDefaultEndpoints();

app.Run();

// Make Program class accessible for testing
public partial class Program { }
