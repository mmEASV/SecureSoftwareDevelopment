# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Quick Commands

**Build and run:**
```bash
# Build entire solution
dotnet build

# Run with .NET Aspire (recommended - orchestrates all services)
cd src/AppHost
dotnet run
# Access Aspire Dashboard at https://localhost:15068

# Run individual services (alternative approach)
dotnet run --project src/Template.Api/Template.Api.csproj
dotnet run --project src/Template.Web/Template.Web.csproj
```

**Testing:**
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/Template.Api.Tests/Template.Api.Tests.csproj

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"
```

**Database migrations:**
```bash
# Add new migration (from Template.Api directory)
cd src/Template.Api
dotnet ef migrations add <MigrationName>

# Apply migrations to database
dotnet ef database update

# Revert to specific migration
dotnet ef database update <MigrationName>

# Remove last migration (if not applied)
dotnet ef migrations remove
```

**Docker:**
```bash
# Start PostgreSQL via docker-compose
docker-compose -f src/compose.yaml up postgres

# Build and run all services with docker-compose
docker-compose -f src/compose.yaml up
```

## Architecture Overview

This is a .NET 9.0 Aspire application with a clean separation between frontend and backend:

### Project Structure

- **AppHost/** - .NET Aspire orchestration host; main entry point for running the entire application
- **Template.Api/** - ASP.NET Core Web API backend with minimal API endpoints
- **Template.Web/** - Blazor WebAssembly frontend (client-side)
- **Template.Shared/** - Shared DTOs, models, and domain entities used by both API and Web
- **ServiceDefaults/** - Common Aspire service configurations (logging, health checks, telemetry)

### Backend Architecture (Template.Api)

The API follows Clean Architecture principles with clear separation of concerns.

**Architectural Reasoning:**

This structure was chosen to provide:

1. **Clear Separation of Concerns**: Each layer has a single, well-defined responsibility
   - **Endpoints**: Handle HTTP concerns (routing, request/response mapping)
   - **Application**: Orchestrate business logic and coordinate between layers
   - **Domain**: Define core business rules and contracts (independent of infrastructure)
   - **Infrastructure**: Implement technical details (database, external services)
   - **Configuration**: Centralize app settings and cross-cutting concerns

2. **Dependency Flow (Clean Architecture)**:
   ```
   Endpoints → Application → Domain
                          ↓
                   Infrastructure
   ```
   - Domain has NO dependencies (pure business logic)
   - Application depends on Domain interfaces
   - Infrastructure implements Domain interfaces
   - Endpoints depend on Application services
   - This ensures business logic is not coupled to frameworks or databases

3. **Feature-Based Organization** (Endpoints):
   - Auth/, Users/, Tenants/ folders make features easy to find
   - New features can be added without touching unrelated code
   - Clear ownership boundaries for each feature area

4. **Testability**:
   - Domain logic can be tested without database or HTTP concerns
   - Services depend on interfaces, making mocking straightforward
   - Repository pattern allows in-memory testing

5. **No CQRS Overhead**:
   - Simple Service pattern chosen over CQRS (no MediatR)
   - CQRS adds complexity that's not needed for CRUD-heavy APIs
   - Keeps the codebase accessible and easy to understand

6. **Infrastructure Isolation**:
   - Database concerns (EF Core, migrations, interceptors) contained in Infrastructure/Persistence
   - JWT implementation in Infrastructure/Identity (not in Domain)
   - Easy to swap implementations (e.g., PostgreSQL → SQL Server)

7. **Configuration Centralization**:
   - Settings models in Configuration/Settings (not scattered across layers)
   - Middleware in Configuration/Middleware for easy discovery
   - Cross-cutting concerns in one place

This structure scales well from small teams to enterprise applications while remaining simple enough to understand quickly.

**Where to Place New Code:**

- **Add to Endpoints/** when:
  - Creating new HTTP endpoints
  - Defining API routes and HTTP handlers
  - Example: Adding a new "Products" feature → create `Endpoints/Products/ProductEndpoints.cs`

- **Add to Application/Services/** when:
  - Implementing business logic
  - Orchestrating multiple repositories
  - Coordinating cross-cutting concerns
  - Example: UserService validates tenant, calls repository, handles business rules

- **Add to Application/Common/Interfaces/** when:
  - Defining contracts for services
  - Creating abstractions for business operations
  - Example: IProductService defines what operations are available for products

- **Add to Domain/Interfaces/** when:
  - Defining data access contracts (repositories)
  - Creating abstractions for infrastructure concerns
  - Example: IProductRepository defines how products are persisted

- **Add to Domain/Exceptions/** when:
  - Creating domain-specific exceptions
  - Defining error conditions from business logic
  - Example: ProductOutOfStockException for inventory constraints

- **Add to Infrastructure/Persistence/** when:
  - Implementing database configurations
  - Creating EF Core entity configurations
  - Adding interceptors or conventions

- **Add to Infrastructure/Repositories/** when:
  - Implementing data access logic
  - Creating repository classes that implement Domain interfaces
  - Example: ProductRepository implements IProductRepository

- **Add to Infrastructure/** (new subfolder) when:
  - Integrating external services (email, payment gateways)
  - Implementing third-party API clients
  - Example: Infrastructure/Email/EmailService.cs

- **Add to Configuration/** when:
  - Creating new settings classes
  - Adding middleware that affects all requests
  - Defining cross-cutting behavior

**Layers:**
- **Endpoints/** - API layer (Minimal API endpoints organized by feature)
  - **Auth/** - Authentication endpoints (Login, RefreshToken, ChangeTenant)
  - **Users/** - User CRUD endpoints
  - **Tenants/** - Tenant CRUD endpoints
- **Application/** - Business logic layer
  - **Services/** - Business service implementations (UserService, TenantService)
  - **Common/**
    - **Interfaces/** - Service contracts (IUserService, ITenantService, IJwtService)
    - **Mappings/** - AutoMapper profiles and converters
  - **Extensions/** - Service registration (ServiceCollectionExtensions, WebApplicationExtensions)
- **Domain/** - Domain logic (API-specific, not shared)
  - **Common/** - CurrentContext, ErrorResponse
  - **Interfaces/** - Repository interfaces (IUserRepository, ITenantRepository)
  - **Exceptions/** - Domain exceptions (NotFoundException, BadRequestException, CustomException)
  - **Errors/** - Error code constants
- **Infrastructure/** - Data access & external services
  - **Persistence/** - EF Core configuration
    - **ApplicationDbContext.cs** - Main DbContext with query filters and interceptors
    - **Conventions/** - GuidV7 convention for primary keys
    - **Interceptors/** - Soft delete interceptor
    - **Migrations/** - EF Core migrations
    - **EfCoreExtensions.cs** - Query filter helpers
  - **Repositories/** - Repository implementations (UserRepository, TenantRepository)
  - **Identity/** - JWT token service implementation
  - **Initialization/** - Database seeding (DbInitializer)
- **Configuration/** - Application configuration
  - **Settings/** - Configuration models (JwtSettings, GlobalSettings)
  - **Middleware/** - Global middleware (CurrentContextMiddleware, GlobalExceptionHandler)

**Key Patterns & Conventions:**

- **Clean Architecture**: Clear dependency flow (Domain ← Application ← Infrastructure)
- **Minimal APIs**: Endpoints defined as static extension methods, grouped by feature
- **Service Pattern**: Business logic in services, no CQRS complexity
- **Repository Pattern**: Data access abstraction
- **JWT Authentication**: Token-based auth with refresh token support
- **Multi-tenancy**: Tenant isolation via JWT claims and EF Core query filters
- **Soft Delete**: Automatic soft delete via interceptor
- **AutoMapper**: DTO to entity mapping
- **FluentValidation**: Request validation

**Naming Conventions:**

- **Endpoints**: `{Feature}Endpoints.cs` (e.g., `UserEndpoints.cs`, `AuthEndpoints.cs`)
- **Services**: `{Entity}Service.cs` implementing `I{Entity}Service` (e.g., `UserService : IUserService`)
- **Repositories**: `{Entity}Repository.cs` implementing `I{Entity}Repository`
- **Exceptions**: `{ErrorType}Exception.cs` (e.g., `NotFoundException`, `BadRequestException`)
- **Settings**: `{Feature}Settings.cs` (e.g., `JwtSettings`, `EmailSettings`)
- **Middleware**: `{Purpose}Middleware.cs` (e.g., `CurrentContextMiddleware`)
- **Extension Methods**: `{Type}Extensions.cs` for registration (e.g., `ServiceCollectionExtensions`, `WebApplicationExtensions`)

**File Organization Rules:**

1. **One endpoint file per feature** - Keep all related endpoints together
2. **Interfaces with implementations** - Service interfaces in Common/Interfaces, implementations in Services
3. **Group by feature, not by type** - Endpoints/Products/ (not Endpoints/GetProducts/, Endpoints/CreateProducts/)
4. **Keep migrations in Infrastructure/Persistence/Migrations** - Don't move or rename migration files
5. **Settings registered in Program.cs** - All Configuration/Settings/*.cs files should be registered via Configure<T>()

### Frontend Architecture (Template.Web)

Blazor WebAssembly application with component-based UI:

**Structure:**
- **Pages/** - Routable page components organized by feature (Identity, Tenants, Profile)
- **Shared/** - Reusable components and layouts
  - **Layout/** - App layout components
  - **Components/** - Shared UI components
- **Application/Services/** - Client-side services
  - **HttpService** - Centralized HTTP client with JWT auth, token refresh, and error handling
  - **AuthenticationService** - Manages authentication state
  - **TenantRouteService** - Multi-tenant routing

**UI Libraries:**
- Microsoft FluentUI Components (primary)
- Blazor Bootstrap
- Additional: Blazored.Modal, Blazored.Toast, Blazored.LocalStorage

### Multi-Tenancy

The application supports multi-tenancy via JWT claims:
- Tenant ID stored in JWT as custom claim (`AppClaimTypes.TenantIdentifier`)
- `CurrentContext` on API side extracts tenant info from authenticated user
- Admin users can switch tenants via `/Identity/Auth/ChangeTenant/{tenantId}`

### Authentication Flow

1. User logs in via `/Identity/Auth/Login` with email/password
2. API returns JWT token + refresh token in `AuthResponse`
3. Web client stores tokens in LocalStorage
4. `HttpService` automatically attaches JWT to API requests
5. On 401 response, `HttpService` attempts token refresh before logging out
6. Refresh tokens expire after 7 days

### .NET Aspire Orchestration

The `AppHost/Program.cs` configures service dependencies:
- PostgreSQL container with persistent volume
- API references the database connection
- Web references the API
- All services automatically registered with health checks and telemetry

### Shared Models (Template.Shared)

Common types shared between API and Web:
- **Models/** - Domain entities (User, Tenant, ApplicationUser, ApplicationRole)
- **Dto/** - Data transfer objects for API requests/responses
- **Models/Interfaces/** - Common interfaces (ITrackable, ISoftDeletable, IOwned)
- **Utils/** - Shared utilities (PaginatedList, JsonExtensions)

## Development Notes

- **Database:** PostgreSQL via .NET Aspire (automatic container management) or docker-compose
- **EF Core:** Migrations tracked in Template.Api; DbInitializer seeds data in development
- **Testing:** xUnit with NSubstitute for mocking
- **Logging:** Serilog with structured logging and request enrichment (IP, User-Agent)
- **CORS:** Development policy allows any origin; Production policy requires configured FrontEndUrl
- **Configuration:** Settings in appsettings.json; Jwt:Key required for authentication
- **Container Support:** Multi-arch builds (linux-x64, linux-arm64) configured in project files
