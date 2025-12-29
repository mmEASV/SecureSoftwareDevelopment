namespace Template.Shared.Models.Interfaces;

public interface IOwned
{
    public Guid OwnerId { get; set; }
    public Tenant? Owner { get; set; }
}
