using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// Setup PostgreSQL
var username = builder.AddParameter("username", secret: true);
var password = builder.AddParameter("password", secret: true);

var postgres = builder.AddPostgres("TemplatePostgreSql", username, password, port: 5432)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);
var postgresdb = postgres.AddDatabase("TemplateWriteDb");

// Setup API
var api = builder.AddProject<Template_Api>("api")
    .WithReference(postgresdb, "DefaultConnection")
    .WaitFor(postgresdb);

// Setup Web
builder.AddProject<Template_Web>("web")
    .WithExternalHttpEndpoints()
    .WithReference(api);

builder.Build().Run();