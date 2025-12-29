using Blazored.LocalStorage;
using Template.Web.Application.Services;

namespace Template.Web.Application.Extension;

public static class ServicesAndRepositoryExtension
{
    public static IServiceCollection AddServicesAndRepositories(this IServiceCollection services)
    {
        #region Repository



        #endregion
        #region Service

        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IHttpService, HttpService>();
        services.AddScoped<TenantRouteService>();

        #endregion

        services.AddBlazoredLocalStorage();

        return services;
    }
}
