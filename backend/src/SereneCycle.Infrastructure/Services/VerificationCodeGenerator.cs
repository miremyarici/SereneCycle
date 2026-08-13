using System.Security.Cryptography;
using System.Text;

namespace SereneCycle.Infrastructure.Services;

/// <summary>
/// 6 haneli doğrulama kodlarının üretimi ve doğrulanması.
///
/// Kod veritabanında düz metin olarak tutulmaz, SHA-256 hash'i tutulur:
/// veritabanı sızarsa bekleyen kodlar kullanılamaz. Karşılaştırma sabit
/// zamanlı yapılır (timing attack'e karşı).
/// </summary>
public static class VerificationCodeGenerator
{
    public const int CodeLength = 6;

    /// <summary>Kodun geçerlilik süresi.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);

    /// <summary>Kod yenilenmeden önce izin verilen yanlış deneme sayısı.</summary>
    public const int MaxAttempts = 5;

    /// <summary>
    /// Kriptografik olarak güvenli 6 haneli kod. <c>Random</c> değil
    /// <c>RandomNumberGenerator</c> kullanılır — kod tahmin edilebilir olmamalı.
    /// </summary>
    public static string Generate()
    {
        var value = RandomNumberGenerator.GetInt32(0, 1_000_000);
        return value.ToString("D6");
    }

    public static string Hash(string code)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code));
        return Convert.ToBase64String(bytes);
    }

    public static bool Verify(string code, string? expectedHash)
    {
        if (string.IsNullOrEmpty(expectedHash))
        {
            return false;
        }

        var actual = Encoding.UTF8.GetBytes(Hash(code));
        var expected = Encoding.UTF8.GetBytes(expectedHash);

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
