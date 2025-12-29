namespace Template.Shared.Dto.File;

public class UploadDto
{
    public required Guid Id { get; set; }
    public required string Url { get; set; }
    public string Name { get; set; } = "";
}
