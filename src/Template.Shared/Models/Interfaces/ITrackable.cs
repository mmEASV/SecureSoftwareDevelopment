namespace Template.Shared.Models.Interfaces;

public interface ITrackable
{
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
