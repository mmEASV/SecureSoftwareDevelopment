using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using FluentValidation.AspNetCore;
using Template.Api.Application.Extensions;
using Template.Api.Application.Common.Mappings;
using Template.Api.Configuration.Settings;
using Template.Api.Configuration.Middleware;
using Template.Api.Domain.Common;
using Template.Api.Infrastructure.Persistence;
using Template.Api.Infrastructure.Initialization;
using Template.Shared;
using Template.Shared.Dto;
using Template.Shared.Models.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add serilog
builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

builder.AddServiceDefaults();

// Register Options
builder.Services.Configure<GlobalSettings>(builder.Configuration.GetSection("Global"));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

// Register DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution);
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddDefaultIdentity<ApplicationUser>()
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Register Authentication
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        var jwtConfig = builder.Configuration.GetSection("Jwt");
        var key = Encoding.UTF8.GetBytes(jwtConfig.GetValue<string>("Key")
                                         ?? throw new NullReferenceException("JWT key cannot be null"));
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtConfig.GetValue<string>("Issuer"),
            ValidAudience = jwtConfig.GetValue<string>("Audience"),
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

// Add services to the container.
builder.Services.AddServicesAndRepositories();

// Create a method to initialize the database
async Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
    await initializer.InitializeAsync();
}


// Add fluent validation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining(typeof(LoginRequestValidator));

// Add automapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Add API docs
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Add CORS
builder.Services.AddCors(options =>
{
    var globalConfig = builder.Configuration.GetSection("Global");
    options.AddPolicy(name: "Production",
        policy =>
        {
            policy.WithOrigins(globalConfig.GetValue<string>("FrontEndUrl")
                               ?? throw new NullReferenceException("FrontEndUrl cannot be null"))
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    options.AddPolicy(name: "Development",
        policy =>
        {
            policy.AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
                .SetIsOriginAllowed(_ => true);
        });
});


var app = builder.Build();

app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (
        diagnosticContext,
        httpContext) =>
    {
        if (httpContext.Connection.RemoteIpAddress is not null)
        {
            diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress);
        }
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent);
    };
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseCors("Development");
    app.MapOpenApi();

    await InitializeDatabaseAsync(app);
}
else
{
    app.UseCors("Production");
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<CurrentContextMiddleware>();

// Register Apis
app.AddApis();

// Map Healthchecks
app.MapDefaultEndpoints();

app.Run();