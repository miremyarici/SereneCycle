using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SereneCycle.Domain.Entities;

namespace SereneCycle.Infrastructure.Persistence;

/// <summary>
/// Katalogla veritabanını hizalar. Bilinçli olarak
/// <see cref="DevelopmentDataSeeder"/>'dan ayrı: içerik kataloğu <b>örnek
/// veri değil</b>, uygulamanın çalışması için gereken referans veridir.
/// Test kullanıcısı ve örnek günlükler kapatılabilir; katalog kapatılırsa
/// Beslenme ve Hareket ekranları boş kalır.
///
/// Belirtiler (<see cref="SymptomSeedData"/>) migration içinde
/// <c>HasData</c> ile gidiyor; katalog için aynı yol seçilmedi çünkü her
/// içerik düzeltmesi yeni bir migration gerektirirdi ve katalogun yüzlerce
/// satıra büyümesi bekleniyor. Buradaki uzlaşma: şema migration'la,
/// editoryal içerik açılışta hizalanan seed verisiyle taşınır.
///
/// Idempotent ve üç yönlü çalışır — eksikleri ekler, var olanları tazeler,
/// katalogdan çıkarılmışları siler. Şema güncel olmalıdır; bu yüzden
/// migration'ların uygulanmasından <b>sonra</b> çağrılır.
/// </summary>
public static class ContentCatalogSeeder
{
    public static async Task SyncAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        var db = services.GetRequiredService<AppDbContext>();
        var logger = services
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(ContentCatalogSeeder));

        // Eşleştirme (faz, tür, başlık) üçlüsüyle yapılır; Id kimliğin
        // kendisi değil, veritabanının ürettiği bir ayrıntıdır.
        var existing = await db.ContentItems.ToDictionaryAsync(
            item => (item.Phase, item.Type, item.Title), cancellationToken);

        var added = 0;

        foreach (var seed in ContentSeedData.All)
        {
            var key = (seed.Phase, seed.Type, seed.Title);

            if (existing.Remove(key, out var current))
            {
                RefreshFrom(current, seed);
                continue;
            }

            db.ContentItems.Add(seed);
            added++;
        }

        // Sözlükte kalanlar katalogda karşılığı olmayan satırlardır. Silme
        // adımı şart: yalnızca ekleyen bir seeder'da yeniden adlandırılan
        // öğenin eski satırı veritabanında kalır ve öneri motoru onu aday
        // olarak görmeye devam eder — katalog artık o metnin insan
        // denetiminden geçtiğini iddia edemiyor olsa bile.
        // Ara liste yok: sözlüğün değerleri doğrudan veriliyor.
        var removedCount = existing.Count;
        db.ContentItems.RemoveRange(existing.Values);

        if (!db.ChangeTracker.HasChanges())
        {
            return;
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Faz içerik kataloğu hizalandı: {Added} eklendi, {Removed} "
            + "kaldırıldı, {Total} toplam öğe.",
            added, removedCount, ContentSeedData.All.Count);
    }

    /// <summary>
    /// Metin ve maskeler her açılışta tazelenir: geliştirici veritabanı
    /// katalogun eski bir sürümüyle doldurulmuş olabilir ve maskeler
    /// eskirse sert filtre yanlış şeyi eler.
    /// </summary>
    private static void RefreshFrom(ContentItem current, ContentItem seed)
    {
        current.Body = seed.Body;
        current.TagMask = seed.TagMask;
        current.ContraMask = seed.ContraMask;
        current.DurationMinutes = seed.DurationMinutes;
    }
}
