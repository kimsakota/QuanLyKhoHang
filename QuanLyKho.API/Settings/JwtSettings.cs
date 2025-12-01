namespace QuanLyKho.API.Settings;

public class JwtSettings
{
    public string Issuer { get; set; } = "QuanLyKho.Api";
    public string Audience { get; set; } = "QuanLyKho.Client";
    public string SigningKey { get; set; } = "super-secret-signing-key-change-me";
    public int AccessTokenMinutes { get; set; } = 60;
}
