using FluentValidation;

namespace Admin.Shared.Dto;

public class LoginRequest
{
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotNull().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}
