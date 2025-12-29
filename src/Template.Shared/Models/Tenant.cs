using System.Collections.ObjectModel;
using Template.Shared.Models.Identity;
using Template.Shared.Models.Interfaces;

namespace Template.Shared.Models;

public class Tenant : ISoftDeletable
{
    public Guid Id { get; set; }
    public required string Name { get; set; }

    // Interfaces
    public DateTimeOffset? DeletedAt { get; set; }
}