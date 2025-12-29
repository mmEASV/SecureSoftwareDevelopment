namespace Template.Api.Domain.Errors;

public static class ErrorCodes
{
    // Auth
    public const string WrongUsernameOrPassword = "Wrong username or password";
    public const string AccountLockedOut = "Account is currently locked out. Try again later.";
    public const string EmailNotConfirmed = "Please verify your email before logging in.";
    public const string EmailVerifyInvalidIdOrToken = "Could not verify email.";
    public const string RecoveryCodeNotValid = "Invalid recovery code.";

    // Token
    public const string InvalidToken = "Invalid token";
    public const string InvalidRefreshToken = "Invalid refresh token";
}
