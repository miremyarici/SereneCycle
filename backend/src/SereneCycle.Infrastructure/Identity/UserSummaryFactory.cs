using System.Linq.Expressions;
using SereneCycle.Application.Auth;

namespace SereneCycle.Infrastructure.Identity;

/// <summary>
/// <see cref="AppUser"/> → <see cref="UserSummary"/> eşlemesinin tek yeri.
/// Auth ve profil uçlarının ikisi de aynı özeti döndürüyor; eşleme iki yerde
/// yazılırsa alanlardan biri er geç yalnızca bir uçta güncellenir.
///
/// Eşleme bilinçli olarak <see cref="Expression"/> olarak tanımlı: aynı
/// tanım hem veritabanı projeksiyonu olarak (yalnızca gereken kolonlar
/// okunur, avatar baytları ağa hiç çıkmaz) hem de elde hazır bir varlık
/// için derlenmiş hâliyle kullanılabiliyor.
/// </summary>
internal static class UserSummaryFactory
{
    /// <summary>
    /// EF projeksiyonu. <c>HasCompletedOnboarding</c> ayrı bir sorgudan
    /// geldiği için burada varsayılan <c>false</c> kalır ve çağıran
    /// tarafından <c>with</c> ile doldurulur.
    /// </summary>
    public static readonly Expression<Func<AppUser, UserSummary>> Projection =
        user => new UserSummary(
            user.Id,
            user.Name,
            user.Email!,
            user.EmailConfirmed,
            user.AvgCycleLength,
            user.AvgPeriodLength,
            false,
            user.AvatarUpdatedAt);

    private static readonly Func<AppUser, UserSummary> Map = Projection.Compile();

    /// <summary>Zaten belleğe alınmış bir kullanıcıdan özet üretir.</summary>
    public static UserSummary From(AppUser user, bool hasCompletedOnboarding) =>
        Map(user) with { HasCompletedOnboarding = hasCompletedOnboarding };
}
