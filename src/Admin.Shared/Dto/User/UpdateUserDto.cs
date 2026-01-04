using System.ComponentModel.DataAnnotations;

namespace Admin.Shared.Dto.User;

public class UpdateUserDto
{
    public string Name { get; set; } = string.Empty;

    public string Surname { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public Guid? TenantId { get; set; }
}