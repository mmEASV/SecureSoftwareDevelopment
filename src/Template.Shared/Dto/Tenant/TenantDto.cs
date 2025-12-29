using System.Collections.ObjectModel;
using Template.Shared.Dto.User;

namespace Template.Shared.Dto.Tenant;

public class TenantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;

    public IEnumerable<UserDto> Users { get; set; } = new Collection<UserDto>();
}
