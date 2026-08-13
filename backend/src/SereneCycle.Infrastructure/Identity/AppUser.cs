using Microsoft.AspNetCore.Identity;

namespace SereneCycle.Infrastructure.Identity;

/// <summary>
/// Identity kullanıcısı + uygulamaya özel profil alanları.
/// Identity'ye bağımlı olduğu için Domain'de değil Infrastructure'da:
/// Domain katmanı framework'ten bağımsız kalsın diye.
/// </summary>
public class AppUser : IdentityUser<Guid>
{
    public required string Name { get; set; }

    /// <summary>Onboarding'de sorulur, profilden değiştirilebilir.</summary>
    public int AvgCycleLength { get; set; } = 28;

    public int AvgPeriodLength { get; set; } = 5;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 6 haneli doğrulama/sıfırlama kodunun hash'i. Kodun kendisi asla
    /// saklanmaz — veritabanı sızarsa kodlar kullanılamasın diye.
    /// </summary>
    public string? VerificationCodeHash { get; set; }

    public DateTimeOffset? VerificationCodeExpiresAt { get; set; }

    /// <summary>Kaba kuvvet denemelerini sınırlamak için.</summary>
    public int VerificationAttemptCount { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
