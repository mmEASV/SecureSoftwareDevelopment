using Template.Api.Endpoints.Auth;
using Template.Api.Endpoints.Tenants;
using Template.Api.Endpoints.Users;

namespace Template.Api.Application.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication AddApis(this WebApplication app)
    {
        app.AddAuthApi();
        app.AddTenantApi();
        app.AddUserApi();

        return app;
    }
}
