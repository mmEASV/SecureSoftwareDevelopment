using System.Collections.ObjectModel;
using Admin.Shared.Dto.User;

namespace Admin.Shared.Dto.Tenant;

public class TenantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;

    public IEnumerable<UserDto> Users { get; set; } = new Collection<UserDto>();
}
