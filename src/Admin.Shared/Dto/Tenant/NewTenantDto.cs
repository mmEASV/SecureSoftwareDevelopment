using FluentValidation;

namespace Admin.Shared.Dto.Tenant;

public class NewTenantDto
{
    public string Name { get; set; } = null!;
}

public class NewTenantDtoValidator : AbstractValidator<NewTenantDto>
{
    public NewTenantDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}
