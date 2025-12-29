using System.Security.Claims;
using Template.Shared.Models.Identity.Claims;

namespace Template.Api.Domain.Common;

public class CurrentContext
{
    public HttpContext HttpContext { get; private set; } = null!;
    public Guid? UserId { get; private set; }
    public List<string>? Roles { get; private set; }
    public string? UserAgent { get; private set; }
    public Guid? TenantId { get; set; }

    public void Build(HttpContext httpContext)
    {
        HttpContext = httpContext;
        UserAgent = GetUserAgent(httpContext);

        SetUser(httpContext.User);
        SetTenant(httpContext.User);
    }

    private void SetUser(ClaimsPrincipal user)
    {
        if (user.Identity is ClaimsIdentity identity)
        {
            if (identity.Claims.Any())
            {
                var userId = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                UserId = userId is null
                    ? null
                    : Guid.Parse(userId);
                Roles = identity.Claims
                    .Where(c => c.Type == ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToList();
            }
        }
    }

    private void SetTenant(ClaimsPrincipal user)
    {
        if (user.Identity is ClaimsIdentity identity)
        {
            if (identity.Claims.Any())
            {
                var tenantId = identity.FindFirst(AppClaimTypes.TenantIdentifier)?.Value;
                TenantId = tenantId is null
                    ? null
                    : Guid.Parse(tenantId);
            }
        }
    }

    public bool IsAdmin()
    {
        return Roles?.Any(r => string.Equals(r, "admin", StringComparison.OrdinalIgnoreCase))
               ?? false;
    }

    private static string? GetUserAgent(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue("User-Agent", out var header))
        {
            return header.ToString();
        }

        return null;
    }
}
