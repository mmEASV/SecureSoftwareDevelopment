namespace Template.Shared.Dto;

public class AuthResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = default!;
    public string Token { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
    public List<string> Roles { get; set; } = new List<string>();
}
