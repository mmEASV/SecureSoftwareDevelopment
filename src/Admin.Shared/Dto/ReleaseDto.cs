namespace Admin.Shared.Dto;

public record ReleaseDto
{
    public Guid Id { get; init; }
    public Guid UpdateId { get; init; }
    public UpdateDto? Update { get; init; }
    public DateTime ReleaseDate { get; init; }
    public bool IsActive { get; init; }
    public bool IsMandatory { get; init; }
    public string? MinimumVersion { get; init; }
    public int MaxPostponeDays { get; init; }
    public string? ReleaseNotes { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class CreateReleaseDto
{
    public Guid UpdateId { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public bool IsMandatory { get; set; }
    public string? MinimumVersion { get; set; }
    public int MaxPostponeDays { get; set; } = 7;
    public string? ReleaseNotes { get; set; }
}

public class UpdateReleaseDto
{
    public bool? IsActive { get; set; }
    public bool? IsMandatory { get; set; }
    public string? ReleaseNotes { get; set; }
}
