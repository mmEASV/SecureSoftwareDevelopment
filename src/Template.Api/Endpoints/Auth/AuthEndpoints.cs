using System.Security.Claims;
using Template.Shared.Dto;
using Template.Shared.Models.Identity;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Template.Api.Domain.Common;
using Template.Api.Domain.Errors;
using Template.Api.Application.Common.Interfaces;

namespace Template.Api.Endpoints.Auth;

public static class AuthEndpoints
{
    public static RouteGroupBuilder AddAuthApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/Identity/Auth");

        api.MapPost("/Login", Login)
            .WithName("Login")
            .AllowAnonymous();

        api.MapPost("/ChangeTenant/{tenantId:guid}", ChangeTenant)
            .WithName("Change Tenant")
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        api.MapPost("/RefreshToken", RefreshToken)
            .WithName("Refresh Token")
            .AllowAnonymous();

        return api;
    }

    public static async Task<Results<Ok<AuthResponse>, BadRequest<ErrorResponse>, ProblemHttpResult>> Login(
        UserManager<ApplicationUser> userManager,
        IJwtService jwtService,
        [FromBody] LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            return TypedResults.BadRequest(new ErrorResponse(
                nameof(ErrorCodes.WrongUsernameOrPassword),
                StatusCodes.Status400BadRequest,
                ErrorCodes.WrongUsernameOrPassword));
        }

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return TypedResults.BadRequest(new ErrorResponse(
                nameof(ErrorCodes.WrongUsernameOrPassword),
                StatusCodes.Status400BadRequest,
                ErrorCodes.WrongUsernameOrPassword));
        }

        var result = await userManager.CheckPasswordAsync(user, request.Password);
        if (!result)
        {
            return TypedResults.BadRequest(new ErrorResponse(
                nameof(ErrorCodes.WrongUsernameOrPassword),
                StatusCodes.Status400BadRequest,
                ErrorCodes.WrongUsernameOrPassword));
        }

        var roles = await userManager.GetRolesAsync(user);
        var token = jwtService.GenerateJwtToken(user, roles, null);

        return TypedResults.Ok(new AuthResponse()
        {
            Email = user.Email!,
            UserId = user.Id,
            Token = token,
            Roles = roles.ToList()
        });
    }

    public static async Task<Results<Ok<AuthResponse>, BadRequest<ErrorResponse>, NotFound, ProblemHttpResult>>
        ChangeTenant(
            UserManager<ApplicationUser> userManager,
            IJwtService jwtService,
            CurrentContext currentContext,
            ITenantService tenantService,
            [FromRoute] Guid tenantId)
    {
        var user = await userManager.FindByIdAsync(currentContext.UserId.ToString()!);
        if (user == null)
        {
            return TypedResults.BadRequest(new ErrorResponse(
                nameof(ErrorCodes.WrongUsernameOrPassword),
                StatusCodes.Status400BadRequest,
                ErrorCodes.WrongUsernameOrPassword));
        }

        var tenant = await tenantService.Get(tenantId);
        if (tenant is null)
        {
            return TypedResults.NotFound();
        }

        var roles = await userManager.GetRolesAsync(user);
        var token = jwtService.GenerateJwtToken(user, roles, null, tenantId);

        return TypedResults.Ok(new AuthResponse()
        {
            Email = user.Email!,
            UserId = user.Id,
            Token = token,
            Roles = roles.ToList()
        });
    }

    public static async Task<Results<Ok<AuthResponse>, BadRequest<ErrorResponse>, NotFound, ProblemHttpResult>>
        RefreshToken(
            UserManager<ApplicationUser> userManager,
            IJwtService jwtService,
            [FromBody] TokenRequest request)
    {
        var principal = jwtService.GetPrincipalFromExpiredToken(request.Token);
        if (principal == null)
        {
            return TypedResults.BadRequest(new ErrorResponse(
                nameof(ErrorCodes.InvalidToken),
                StatusCodes.Status400BadRequest,
                ErrorCodes.InvalidToken));
        }

        var userId = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
        {
            return TypedResults.NotFound();
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user == null ||
            user.RefreshToken != request.RefreshToken ||
            user.RefreshTokenExpiryTime <= DateTime.Now)
        {
            return TypedResults.BadRequest(new ErrorResponse(
                nameof(ErrorCodes.InvalidRefreshToken),
                StatusCodes.Status400BadRequest,
                ErrorCodes.InvalidRefreshToken));
        }

        // Issue new token
        var roles = await userManager.GetRolesAsync(user);
        var newJwtToken = jwtService.GenerateJwtToken(user, roles, null);
        var newRefreshToken = jwtService.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.Now.AddMinutes(7 * 1440);
        await userManager.UpdateAsync(user);

        return TypedResults.Ok(new AuthResponse()
        {
            Email = user.Email!,
            UserId = user.Id,
            Token = newJwtToken,
            RefreshToken = newRefreshToken,
            Roles = roles.ToList()
        });
    }
}
