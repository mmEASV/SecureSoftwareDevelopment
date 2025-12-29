using System.Security.Claims;
using Template.Shared.Models.Identity;

namespace Template.Api.Application.Common.Interfaces;

public interface IJwtService
{
    string GenerateJwtToken(ApplicationUser user, IEnumerable<string> roles, IDictionary<string, dynamic>? customClaims, Guid? tenantId = null);

    string GenerateRefreshToken();

    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
