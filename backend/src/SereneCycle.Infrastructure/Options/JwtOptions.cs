using System.ComponentModel.DataAnnotations;

namespace SereneCycle.Infrastructure.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// İmzalama anahtarı. Asla appsettings.json'a yazılmaz — geliştirmede
    /// user-secrets, üretimde ortam değişkeni ile verilir.
    /// HMAC-SHA256 için en az 32 bayt olmalı.
    /// </summary>
    [Required, MinLength(32)]
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Erişim token'ı kısa ömürlü; yenileme refresh token ile yapılır.</summary>
    [Range(1, 1440)]
    public int AccessTokenMinutes { get; set; } = 15;

    [Range(1, 365)]
    public int RefreshTokenDays { get; set; } = 30;
}
