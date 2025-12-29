using Microsoft.AspNetCore.Identity;
using Template.Shared.Models.Interfaces;

namespace Template.Shared.Models.Identity;

public class ApplicationUser : IdentityUser<Guid>, ITrackable, IOwned, ISoftDeletable
{
    public required string Name { get; set; }
    public required string Surname { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }

    // Interfaces
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid OwnerId { get; set; }
    public Tenant? Owner { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
