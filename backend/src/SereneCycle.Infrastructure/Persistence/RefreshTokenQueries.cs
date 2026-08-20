using Microsoft.EntityFrameworkCore;

namespace SereneCycle.Infrastructure.Persistence;

/// <summary>
/// Refresh token'lar üzerinde birden fazla servisin paylaştığı sorgular.
/// </summary>
internal static class RefreshTokenQueries
{
    /// <summary>
    /// Kullanıcının açık bütün oturumlarını düşürür. Şifre hem
    /// <c>AuthService</c> (sıfırlama) hem <c>ProfileService</c> (değiştirme)
    /// üzerinden değişebiliyor; iki yolun da aynı şeyi yapması güvenlik
    /// gereği, bu yüzden sorgu tek yerde duruyor.
    ///
    /// Tek <c>UPDATE</c> ile çalışır: satırlar belleğe alınmaz, iptal edilecek
    /// token sayısından bağımsız olarak tek gidiş-dönüş.
    /// </summary>
    public static Task RevokeAllForUserAsync(
        this AppDbContext db,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(db);

        return db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(
                s => s.SetProperty(t => t.RevokedAt, DateTimeOffset.UtcNow),
                cancellationToken);
    }
}
