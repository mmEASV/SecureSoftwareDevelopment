namespace Template.Shared.Models.Interfaces;

public interface ISoftDeletable
{
    public DateTimeOffset? DeletedAt { get; set; }
}
