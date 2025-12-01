namespace QuanLyKho.API.Models;

public class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
}
