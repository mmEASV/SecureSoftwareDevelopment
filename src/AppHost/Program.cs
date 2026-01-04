using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// Setup PostgreSQL
// Default credentials for development (use user secrets for production)
var username = builder.AddParameter("postgres-username");
var password = builder.AddParameter("postgres-password");

var postgres = builder.AddPostgres("PostgreSql", username, password, port: 5432)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgAdmin();

// Separate databases: ServerDb for vendor/admin side, ClientDb for customer side
var serverDb = postgres.AddDatabase("ServerDb");
var clientDb = postgres.AddDatabase("ClientDb");

// Setup Admin API (Vendor Backend - Upload updates, create releases)
// This API uses ServerDb and will be exposed through Cloudflare tunnel
var adminApi = builder.AddProject<Admin_Api>("admin-api")
    .WithReference(serverDb, "UpdateServiceDb")
    .WaitFor(serverDb);

// Setup Admin Web (Vendor Admin Portal)
builder.AddProject<Admin_Web>("admin-web")
    .WithReference(adminApi);

// Setup Client Portal API (Customer Backend - View releases, manage devices)
// This API uses ClientDb and pulls data from Admin API through tunnel
var clientPortalApi = builder.AddProject<ClientPortal_Api>("client-portal-api")
    .WithReference(clientDb, "UpdateServiceDb")
    .WaitFor(clientDb);

// Setup Client Portal Web (Customer Portal)
builder.AddProject<ClientPortal_Web>("client-portal-web")
    .WithReference(clientPortalApi);

// Setup Client Portal Update Agent (Device Agent)
builder.AddProject<ClientPortal_UpdateAgent>("update-agent")
    .WithReference(clientPortalApi)
    .WaitFor(clientPortalApi);

// Setup Cloudflare Tunnel for secure remote access to Admin API
// The tunnel exposes Admin.Api (ServerDb) to the internet, allowing
// ClientPortal.Api (ClientDb) to pull updates through the tunnel
var tunnelToken = builder.AddParameter("cloudflare-tunnel-token");
var tunnelUrl = builder.AddParameter("cloudflare-tunnel-url"); // e.g., "https://your-tunnel.trycloudflare.com"

builder.AddContainer("cloudflared", "cloudflare/cloudflared", "latest")
    .WithArgs("tunnel", "--no-autoupdate", "run", "--token")
    .WithArgs(tunnelToken)
    .WithEnvironment("TUNNEL_TOKEN", tunnelToken)
    .WaitFor(adminApi);

// Pass tunnel URL to ClientPortal API so it can pull from Admin API
clientPortalApi.WithEnvironment("AdminApi__TunnelUrl", tunnelUrl);

builder.Build().Run();
