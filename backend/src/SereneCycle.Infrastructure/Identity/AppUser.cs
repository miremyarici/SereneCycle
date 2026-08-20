using Microsoft.AspNetCore.Identity;
using SereneCycle.Domain.Content;

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

    /// <summary>
    /// Öneri motorunun sert filtresi: alerji, diyet, sakatlık, hamilelik ve
    /// "elimde yok" denen ekipmanların <see cref="ContraTag"/> maskesi.
    /// Kuralla kesin bilinen bu kısıtlar öğrenilmez — yasaklı öğe, bandit
    /// onu hiç görmeden aday kümesinden çıkarılır.
    /// </summary>
    public long AvoidMask { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 6 haneli doğrulama/sıfırlama kodunun hash'i. Kodun kendisi asla
    /// saklanmaz — veritabanı sızarsa kodlar kullanılamasın diye.
    /// </summary>
    public string? VerificationCodeHash { get; set; }

    public DateTimeOffset? VerificationCodeExpiresAt { get; set; }

    /// <summary>Kaba kuvvet denemelerini sınırlamak için.</summary>
    public int VerificationAttemptCount { get; set; }

    /// <summary>
    /// Profil fotoğrafı. Ayrı bir dosya deposu kurmamak için satır içinde
    /// tutulur; boyut <c>ProfileService</c> tarafından sınırlandırılır.
    /// </summary>
    public byte[]? AvatarData { get; set; }

    public string? AvatarContentType { get; set; }

    public DateTimeOffset? AvatarUpdatedAt { get; set; }

    /// <summary>
    /// Kullanıcının geçmek istediği ama henüz doğrulamadığı adres. Kod
    /// onaylanana kadar <see cref="IdentityUser{TKey}.Email"/> değişmez.
    /// </summary>
    public string? PendingEmail { get; set; }

    public string? EmailChangeCodeHash { get; set; }

    public DateTimeOffset? EmailChangeCodeExpiresAt { get; set; }

    public int EmailChangeAttemptCount { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
