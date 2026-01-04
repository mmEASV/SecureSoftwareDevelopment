using Client.Api.Application.Common.Interfaces;
using Client.Api.Domain.Common;
using Client.Api.Domain.Errors;
using Client.Api.Endpoints.Auth;
using Client.Shared.Dto;
using Client.Shared.Models.Identity;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace Client.Api.Tests.Endpoints;

public class AuthEndpointsTests
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtService _jwtService;

    public AuthEndpointsTests()
    {
        _userManager = Substitute.For<UserManager<ApplicationUser>>(
            Substitute.For<IUserStore<ApplicationUser>>(),
            null, null, null, null, null, null, null, null);

        _jwtService = Substitute.For<IJwtService>();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@test.com",
            Password = "Test@123"
        };

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@test.com",
            UserName = "test@test.com",
            Name = "Test",
            Surname = "User"
        };

        var roles = new List<string> { "User" };
        var token = "generated.jwt.token";

        _userManager.FindByEmailAsync(request.Email).Returns(user);
        _userManager.CheckPasswordAsync(user, request.Password).Returns(true);
        _userManager.GetRolesAsync(user).Returns(roles);
        _jwtService.GenerateJwtToken(user, roles, null, null).Returns(token);

        // Act
        var result = await AuthEndpoints.Login(_userManager, _jwtService, request);

        // Assert
        Assert.IsType<Ok<AuthResponse>>(result.Result);
        var okResult = result.Result as Ok<AuthResponse>;
        Assert.NotNull(okResult);
        Assert.Equal(user.Email, okResult.Value!.Email);
        Assert.Equal(user.Id, okResult.Value.UserId);
        Assert.Equal(token, okResult.Value.Token);
        Assert.Contains("User", okResult.Value.Roles);
    }

    [Fact]
    public async Task Login_WithEmptyEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "",
            Password = "Test@123"
        };

        // Act
        var result = await AuthEndpoints.Login(_userManager, _jwtService, request);

        // Assert
        Assert.IsType<BadRequest<ErrorResponse>>(result.Result);
        var badRequest = result.Result as BadRequest<ErrorResponse>;
        Assert.NotNull(badRequest);
        Assert.Equal(ErrorCodes.WrongUsernameOrPassword, badRequest.Value!.Error);
    }

    [Fact]
    public async Task Login_WithEmptyPassword_ReturnsBadRequest()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@test.com",
            Password = ""
        };

        // Act
        var result = await AuthEndpoints.Login(_userManager, _jwtService, request);

        // Assert
        Assert.IsType<BadRequest<ErrorResponse>>(result.Result);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ReturnsBadRequest()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "nonexistent@test.com",
            Password = "Test@123"
        };

        _userManager.FindByEmailAsync(request.Email).Returns((ApplicationUser?)null);

        // Act
        var result = await AuthEndpoints.Login(_userManager, _jwtService, request);

        // Assert
        Assert.IsType<BadRequest<ErrorResponse>>(result.Result);
        var badRequest = result.Result as BadRequest<ErrorResponse>;
        Assert.NotNull(badRequest);
        Assert.Equal(ErrorCodes.WrongUsernameOrPassword, badRequest.Value!.Error);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsBadRequest()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@test.com",
            Password = "WrongPassword"
        };

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@test.com",
            Name = "Test",
            Surname = "User"
        };

        _userManager.FindByEmailAsync(request.Email).Returns(user);
        _userManager.CheckPasswordAsync(user, request.Password).Returns(false);

        // Act
        var result = await AuthEndpoints.Login(_userManager, _jwtService, request);

        // Assert
        Assert.IsType<BadRequest<ErrorResponse>>(result.Result);
        var badRequest = result.Result as BadRequest<ErrorResponse>;
        Assert.NotNull(badRequest);
        Assert.Equal(ErrorCodes.WrongUsernameOrPassword, badRequest.Value!.Error);
    }

    // Note: ChangeTenant tests are omitted as CurrentContext has private setters
    // that make it difficult to mock properly with NSubstitute

    [Fact]
    public async Task RefreshToken_WithValidToken_ReturnsNewTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var oldToken = "old.jwt.token";
        var oldRefreshToken = "oldRefreshToken";
        var newToken = "new.jwt.token";
        var newRefreshToken = "newRefreshToken";

        var request = new TokenRequest
        {
            Token = oldToken,
            RefreshToken = oldRefreshToken
        };

        var user = new ApplicationUser
        {
            Id = userId,
            Email = "test@test.com",
            RefreshToken = oldRefreshToken,
            RefreshTokenExpiryTime = DateTime.Now.AddDays(1),
            Name = "Test",
            Surname = "User"
        };

        var claims = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString())
            }));

        var roles = new List<string> { "User" };

        _jwtService.GetPrincipalFromExpiredToken(oldToken).Returns(claims);
        _userManager.FindByIdAsync(userId.ToString()).Returns(user);
        _userManager.GetRolesAsync(user).Returns(roles);
        _jwtService.GenerateJwtToken(user, roles, null).Returns(newToken);
        _jwtService.GenerateRefreshToken().Returns(newRefreshToken);
        _userManager.UpdateAsync(user).Returns(IdentityResult.Success);

        // Act
        var result = await AuthEndpoints.RefreshToken(_userManager, _jwtService, request);

        // Assert
        Assert.IsType<Ok<AuthResponse>>(result.Result);
        var okResult = result.Result as Ok<AuthResponse>;
        Assert.NotNull(okResult);
        Assert.Equal(newToken, okResult.Value!.Token);
        Assert.Equal(newRefreshToken, okResult.Value.RefreshToken);
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_ReturnsBadRequest()
    {
        // Arrange
        var request = new TokenRequest
        {
            Token = "invalid.token",
            RefreshToken = "refreshToken"
        };

        _jwtService.GetPrincipalFromExpiredToken(request.Token).Returns((System.Security.Claims.ClaimsPrincipal?)null);

        // Act
        var result = await AuthEndpoints.RefreshToken(_userManager, _jwtService, request);

        // Assert
        Assert.IsType<BadRequest<ErrorResponse>>(result.Result);
        var badRequest = result.Result as BadRequest<ErrorResponse>;
        Assert.NotNull(badRequest);
        Assert.Equal(ErrorCodes.InvalidToken, badRequest.Value!.Error);
    }

    [Fact]
    public async Task RefreshToken_WithExpiredRefreshToken_ReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new TokenRequest
        {
            Token = "valid.jwt.token",
            RefreshToken = "expiredRefreshToken"
        };

        var user = new ApplicationUser
        {
            Id = userId,
            Email = "test@test.com",
            RefreshToken = "expiredRefreshToken",
            RefreshTokenExpiryTime = DateTime.Now.AddDays(-1), // Expired
            Name = "Test",
            Surname = "User"
        };

        var claims = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString())
            }));

        _jwtService.GetPrincipalFromExpiredToken(request.Token).Returns(claims);
        _userManager.FindByIdAsync(userId.ToString()).Returns(user);

        // Act
        var result = await AuthEndpoints.RefreshToken(_userManager, _jwtService, request);

        // Assert
        Assert.IsType<BadRequest<ErrorResponse>>(result.Result);
        var badRequest = result.Result as BadRequest<ErrorResponse>;
        Assert.NotNull(badRequest);
        Assert.Equal(ErrorCodes.InvalidRefreshToken, badRequest.Value!.Error);
    }

    [Fact]
    public async Task RefreshToken_WithMismatchedRefreshToken_ReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new TokenRequest
        {
            Token = "valid.jwt.token",
            RefreshToken = "wrongRefreshToken"
        };

        var user = new ApplicationUser
        {
            Id = userId,
            Email = "test@test.com",
            RefreshToken = "correctRefreshToken",
            RefreshTokenExpiryTime = DateTime.Now.AddDays(1),
            Name = "Test",
            Surname = "User"
        };

        var claims = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString())
            }));

        _jwtService.GetPrincipalFromExpiredToken(request.Token).Returns(claims);
        _userManager.FindByIdAsync(userId.ToString()).Returns(user);

        // Act
        var result = await AuthEndpoints.RefreshToken(_userManager, _jwtService, request);

        // Assert
        Assert.IsType<BadRequest<ErrorResponse>>(result.Result);
        var badRequest = result.Result as BadRequest<ErrorResponse>;
        Assert.NotNull(badRequest);
        Assert.Equal(ErrorCodes.InvalidRefreshToken, badRequest.Value!.Error);
    }
}
