namespace JobPortal.Application.DTOs.Auth;

public class AuthResponseDto
{
    public bool Success { get; set; }

    public string Message { get; set; }
        = default!;

    public string AdminIdentifier { get; set; }
    public string? Token { get; set; }
    public DateTime ExpiresAt { get; set; }

    public string? Role { get; set; }
}