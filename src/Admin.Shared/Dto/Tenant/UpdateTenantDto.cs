using FluentValidation;

namespace Admin.Shared.Dto.Tenant;

public class UpdateTenantDto
{
    public string Name { get; set; } = null!;
}
public class UpdateTenantDtoValidator : AbstractValidator<UpdateTenantDto>
{
    public UpdateTenantDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}
