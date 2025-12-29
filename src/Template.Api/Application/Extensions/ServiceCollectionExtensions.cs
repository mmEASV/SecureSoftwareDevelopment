using Template.Api.Domain.Common;
using Template.Api.Domain.Interfaces;
using Template.Api.Application.Services;
using Template.Api.Application.Common.Interfaces;
using Template.Api.Infrastructure.Initialization;
using Template.Api.Infrastructure.Repositories;
using Template.Api.Infrastructure.Identity;

namespace Template.Api.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServicesAndRepositories(this IServiceCollection services)
    {
        #region Repository

        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        #endregion
        #region Service

        services.AddScoped<IJwtService, JwtTokenService>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<IUserService, UserService>();

        #endregion

        services.AddScoped<CurrentContext>();
        services.AddSingleton(TimeProvider.System);

        // Register DB initializer
        services.AddScoped<DbInitializer>();


        return services;
    }
}
