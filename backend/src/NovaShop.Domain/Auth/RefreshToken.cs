namespace NovaShop.Domain.Auth;

public class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime Expires { get; set; }
    public bool IsExpired => DateTime.UtcNow >= Expires;
    public bool IsRevoked { get; set; } = false;
    public bool IsActive => !IsExpired && !IsRevoked;
}
