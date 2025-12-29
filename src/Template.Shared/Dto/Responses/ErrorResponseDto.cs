namespace Template.Shared.Dto.Responses;

public class ErrorResponseDto
{
    public required string Type { get; init; }
    public int Status { get; init; }
    public string? TraceId { get; init; }
    public required string Error { get; init; }
}
