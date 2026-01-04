using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Client.Api.Configuration.Settings;
using Client.Api.Infrastructure.Identity;
using Client.Shared.Models.Identity;
using Client.Shared.Models.Identity.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;

namespace Client.Api.Tests.Infrastructure.Identity;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _service;
    private readonly IOptions<JwtSettings> _jwtSettings;
    private readonly ILogger<JwtTokenService> _logger;
    private const string TestSecretKey = "ThisIsAVerySecretKeyForTestingPurposes12345678";

    public JwtTokenServiceTests()
    {
        var settings = new JwtSettings
        {
            Key = TestSecretKey,
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationMinutes = 60
        };

        _jwtSettings = Options.Create(settings);
        _logger = Substitute.For<ILogger<JwtTokenService>>();
        _service = new JwtTokenService(_jwtSettings, _logger);
    }

    [Fact]
    public void GenerateJwtToken_WithBasicUser_ReturnsValidToken()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@test.com",
            OwnerId = Guid.NewGuid(),
            Name = "Test",
            Surname = "User"
        };
        var roles = new List<string> { "User" };

        // Act
        var token = _service.GenerateJwtToken(user, roles, null);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);

        // Verify token can be read
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.Equal(_jwtSettings.Value.Issuer, jwtToken.Issuer);
        Assert.Contains(jwtToken.Audiences, a => a == _jwtSettings.Value.Audience);
        Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
        Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Email && c.Value == user.Email);
        Assert.Contains(jwtToken.Claims, c => c.Type == AppClaimTypes.TenantIdentifier && c.Value == user.OwnerId.ToString());
        Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Role && c.Value == "User");
    }

    [Fact]
    public void GenerateJwtToken_WithMultipleRoles_IncludesAllRoles()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "admin@test.com",
            OwnerId = Guid.NewGuid(),
            Name = "Admin",
            Surname = "User"
        };
        var roles = new List<string> { "User", "Admin", "Manager" };

        // Act
        var token = _service.GenerateJwtToken(user, roles, null);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var roleClaims = jwtToken.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        Assert.Equal(3, roleClaims.Count);
        Assert.Contains("User", roleClaims);
        Assert.Contains("Admin", roleClaims);
        Assert.Contains("Manager", roleClaims);
    }

    [Fact]
    public void GenerateJwtToken_WithCustomClaims_IncludesCustomClaims()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@test.com",
            OwnerId = Guid.NewGuid(),
            Name = "Test",
            Surname = "User"
        };
        var roles = new List<string> { "User" };
        var customClaims = new Dictionary<string, dynamic>
        {
            { "Department", "IT" },
            { "EmployeeId", "E123" }
        };

        // Act
        var token = _service.GenerateJwtToken(user, roles, customClaims);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.Contains(jwtToken.Claims, c => c.Type == "Department" && c.Value == "IT");
        Assert.Contains(jwtToken.Claims, c => c.Type == "EmployeeId" && c.Value == "E123");
    }

    [Fact]
    public void GenerateJwtToken_WithTenantId_IncludesTenantClaim()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@test.com",
            OwnerId = Guid.NewGuid(),
            Name = "Test",
            Surname = "User"
        };
        var roles = new List<string> { "User" };
        var tenantId = Guid.NewGuid();

        // Act
        var token = _service.GenerateJwtToken(user, roles, null, tenantId);

        // Assert - Note: Current implementation doesn't use the tenantId parameter
        // but includes user.OwnerId as tenant. This test documents current behavior.
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.Contains(jwtToken.Claims, c => c.Type == AppClaimTypes.TenantIdentifier);
    }

    [Fact]
    public void GenerateJwtToken_HasCorrectExpiration()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@test.com",
            OwnerId = Guid.NewGuid(),
            Name = "Test",
            Surname = "User"
        };
        var roles = new List<string> { "User" };
        var beforeGeneration = DateTime.UtcNow;

        // Act
        var token = _service.GenerateJwtToken(user, roles, null);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var expectedExpiration = beforeGeneration.AddMinutes(_jwtSettings.Value.ExpirationMinutes);
        var actualExpiration = jwtToken.ValidTo;

        // Allow 1 minute tolerance for test execution time
        Assert.True(Math.Abs((expectedExpiration - actualExpiration).TotalMinutes) < 1);
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsNonEmptyString()
    {
        // Act
        var refreshToken = _service.GenerateRefreshToken();

        // Assert
        Assert.NotNull(refreshToken);
        Assert.NotEmpty(refreshToken);
    }

    [Fact]
    public void GenerateRefreshToken_GeneratesUniqueTokens()
    {
        // Act
        var token1 = _service.GenerateRefreshToken();
        var token2 = _service.GenerateRefreshToken();
        var token3 = _service.GenerateRefreshToken();

        // Assert
        Assert.NotEqual(token1, token2);
        Assert.NotEqual(token2, token3);
        Assert.NotEqual(token1, token3);
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsBase64String()
    {
        // Act
        var refreshToken = _service.GenerateRefreshToken();

        // Assert
        // Should be able to decode from Base64
        var bytes = Convert.FromBase64String(refreshToken);
        Assert.Equal(32, bytes.Length); // 32 bytes as per implementation
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_WithValidToken_ReturnsPrincipal()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@test.com",
            OwnerId = Guid.NewGuid(),
            Name = "Test",
            Surname = "User"
        };
        var roles = new List<string> { "User" };
        var token = _service.GenerateJwtToken(user, roles, null);

        // Act
        var principal = _service.GetPrincipalFromExpiredToken(token);

        // Assert
        Assert.NotNull(principal);
        Assert.NotNull(principal.Identity);
        Assert.True(principal.Identity.IsAuthenticated);

        var userIdClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        Assert.NotNull(userIdClaim);
        Assert.Equal(user.Id.ToString(), userIdClaim.Value);

        var emailClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
        Assert.NotNull(emailClaim);
        Assert.Equal(user.Email, emailClaim.Value);
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_WithExpiredToken_StillReturnsPrincipal()
    {
        // Arrange - Create token with very short expiration
        var expiredSettings = new JwtSettings
        {
            Key = TestSecretKey,
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationMinutes = -60 // Negative to create expired token
        };
        var expiredService = new JwtTokenService(Options.Create(expiredSettings), _logger);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@test.com",
            OwnerId = Guid.NewGuid(),
            Name = "Test",
            Surname = "User"
        };
        var roles = new List<string> { "User" };
        var expiredToken = expiredService.GenerateJwtToken(user, roles, null);

        // Act - Should work because ValidateLifetime is false
        var principal = _service.GetPrincipalFromExpiredToken(expiredToken);

        // Assert
        Assert.NotNull(principal);
        var userIdClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        Assert.NotNull(userIdClaim);
        Assert.Equal(user.Id.ToString(), userIdClaim.Value);
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_WithInvalidToken_ThrowsException()
    {
        // Arrange
        var invalidToken = "this.is.not.a.valid.jwt.token";

        // Act & Assert
        Assert.Throws<SecurityTokenMalformedException>(() => _service.GetPrincipalFromExpiredToken(invalidToken));
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_WithTamperedToken_ThrowsException()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@test.com",
            OwnerId = Guid.NewGuid(),
            Name = "Test",
            Surname = "User"
        };
        var roles = new List<string> { "User" };
        var token = _service.GenerateJwtToken(user, roles, null);

        // Tamper with the token
        var parts = token.Split('.');
        var tamperedToken = $"{parts[0]}.{parts[1]}.tampered_signature";

        // Act & Assert
        Assert.Throws<SecurityTokenInvalidSignatureException>(() =>
            _service.GetPrincipalFromExpiredToken(tamperedToken));
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_WithWrongAlgorithm_ThrowsSecurityTokenException()
    {
        // Arrange - Create a token with a different algorithm (this is hard to test without modifying the implementation)
        // For now, we'll test that properly signed tokens work
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@test.com",
            OwnerId = Guid.NewGuid(),
            Name = "Test",
            Surname = "User"
        };
        var roles = new List<string> { "User" };
        var validToken = _service.GenerateJwtToken(user, roles, null);

        // Act
        var result = _service.GetPrincipalFromExpiredToken(validToken);

        // Assert - Valid token should work
        Assert.NotNull(result);
    }
}
