using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SereneCycle.Domain.Cycles;
using SereneCycle.Domain.Entities;
using SereneCycle.Domain.Risk;
using SereneCycle.Infrastructure.Identity;

namespace SereneCycle.Infrastructure.Persistence;

/// <summary>
/// Geliştirme ortamı için örnek veri. Şifre <see cref="UserManager{T}"/>
/// üzerinden yazılır — Identity'nin hash algoritmasını elle taklit etmek
/// yerine gerçek yolu kullanır.
///
/// Idempotent: birden çok kez çalıştırılabilir, var olanı tekrar eklemez.
/// Üretimde asla çağrılmaz (Program.cs ortama göre karar verir).
/// </summary>
public static class DevelopmentDataSeeder
{
    public const string TestUserEmail = "test@serenecycle.app";
    public const string TestUserPassword = "Test1234!";

    public static async Task SeedAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        var logger = services
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(DevelopmentDataSeeder));

        var user = await EnsureTestUserAsync(userManager, logger);
        await EnsureCycleAsync(db, user, logger, cancellationToken);
        await EnsureDailyLogsAsync(db, user, logger, cancellationToken);
        await EnsureContentItemsAsync(db, logger, cancellationToken);

        logger.LogInformation(
            "Geliştirme verisi hazır. Giriş: {Email} / {Password}",
            TestUserEmail, TestUserPassword);
    }

    private static async Task<AppUser> EnsureTestUserAsync(
        UserManager<AppUser> userManager,
        ILogger logger)
    {
        var existing = await userManager.FindByEmailAsync(TestUserEmail);

        if (existing is not null)
        {
            return existing;
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Name = "Test Kullanıcı",
            Email = TestUserEmail,
            UserName = TestUserEmail,
            // Doğrulama akışını atlıyoruz ki e-posta kurulumu olmadan
            // da giriş yapılabilsin.
            EmailConfirmed = true,
            AvgCycleLength = 28,
            AvgPeriodLength = 5
        };

        var result = await userManager.CreateAsync(user, TestUserPassword);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                "Test kullanıcısı oluşturulamadı: "
                + string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        logger.LogInformation("Test kullanıcısı oluşturuldu.");
        return user;
    }

    /// <summary>
    /// Bugünü döngünün 7. gününe denk getirir — tasarım mockup'ındaki
    /// "Day 7 of 28" (foliküler faz) durumuyla aynı.
    /// </summary>
    private static async Task EnsureCycleAsync(
        AppDbContext db,
        AppUser user,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (await db.Cycles.AnyAsync(c => c.UserId == user.Id, cancellationToken))
        {
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var currentStart = today.AddDays(-6);

        db.Cycles.AddRange(
            // Tamamlanmış iki döngü: kayan ortalamanın devreye girmesi için
            // en az 3 tamamlanmış döngü gerekiyor, bu yüzden burada hâlâ
            // onboarding değeri (28) kullanılır.
            new Cycle
            {
                UserId = user.Id,
                StartDate = currentStart.AddDays(-56),
                EndDate = currentStart.AddDays(-28),
                PeriodDays = 5
            },
            new Cycle
            {
                UserId = user.Id,
                StartDate = currentStart.AddDays(-28),
                EndDate = currentStart,
                PeriodDays = 5
            },
            new Cycle
            {
                UserId = user.Id,
                StartDate = currentStart,
                PeriodDays = 5
            });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Örnek döngüler eklendi (bugün = 7. gün).");
    }

    private static async Task EnsureDailyLogsAsync(
        AppDbContext db,
        AppUser user,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (await db.DailyLogs.AnyAsync(l => l.UserId == user.Id, cancellationToken))
        {
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var cramps = await db.Symptoms
            .FirstOrDefaultAsync(
                s => s.Name == "Karın krampları", cancellationToken);
        var tired = await db.Symptoms
            .FirstOrDefaultAsync(s => s.Name == "Yorgunluk", cancellationToken);
        var energetic = await db.Symptoms
            .FirstOrDefaultAsync(s => s.Name == "Enerjik", cancellationToken);

        var firstDay = new DailyLog
        {
            UserId = user.Id,
            LogDate = today.AddDays(-6),
            HasBleeding = true,
            Flow = FlowIntensity.Heavy,
            BloodColor = BloodColor.Red,
            Note = "Döngünün ilk günü."
        };

        var yesterday = new DailyLog
        {
            UserId = user.Id,
            LogDate = today.AddDays(-1),
            HasSpotting = true,
            Note = "Enerjim yerine geldi."
        };

        // Adetin kalan günleri: risk kartının şiddet/renk sayaçlarının
        // gerçekçi bir döngü üzerinde çalıştığı görülebilsin. Bilinçli olarak
        // hiçbir eşiği aşmıyor — geliştirmede kart "temiz" başlıyor.
        var remainingPeriodDays = new[]
        {
            (Offset: -5, Flow: FlowIntensity.Heavy, Color: BloodColor.Red),
            (Offset: -4, Flow: FlowIntensity.Medium, Color: BloodColor.Red),
            (Offset: -3, Flow: FlowIntensity.Light, Color: BloodColor.Brown),
            (Offset: -2, Flow: FlowIntensity.Light, Color: BloodColor.Brown)
        }
            .Select(day => new DailyLog
            {
                UserId = user.Id,
                LogDate = today.AddDays(day.Offset),
                HasBleeding = true,
                Flow = day.Flow,
                BloodColor = day.Color
            })
            .ToList();

        db.DailyLogs.AddRange(firstDay, yesterday);
        db.DailyLogs.AddRange(remainingPeriodDays);
        await db.SaveChangesAsync(cancellationToken);

        var links = new List<LogSymptom>();

        if (cramps is not null)
        {
            links.Add(new LogSymptom
            {
                LogId = firstDay.Id, SymptomId = cramps.Id
            });
        }

        if (tired is not null)
        {
            links.Add(new LogSymptom
            {
                LogId = firstDay.Id, SymptomId = tired.Id
            });
        }

        if (energetic is not null)
        {
            links.Add(new LogSymptom
            {
                LogId = yesterday.Id, SymptomId = energetic.Id
            });
        }

        if (links.Count > 0)
        {
            db.LogSymptoms.AddRange(links);

            // Maske ara tablonun yedeği; gerçek akışta LogService yazıyor,
            // burada elle eklediğimiz için ikisini birlikte tutuyoruz.
            foreach (var log in new[] { firstDay, yesterday })
            {
                log.SymptomMask = SymptomMasks.Of(links
                    .Where(l => l.LogId == log.Id)
                    .Select(l => l.SymptomId));
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("Örnek günlük kayıtlar eklendi.");
    }

    /// <summary>
    /// Faz 2'nin beslenme/hareket içeriği. Dil bilinçli olarak yumuşak:
    /// "yasak" değil "sınırlı tüket", ve her maddede gerekçe var.
    /// </summary>
    private static async Task EnsureContentItemsAsync(
        AppDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (await db.ContentItems.AnyAsync(cancellationToken))
        {
            return;
        }

        db.ContentItems.AddRange(
            // --- Menstrüel ---
            Item(CyclePhase.Menstrual, ContentType.FoodDo, "Ispanak",
                "Demir açısından zengin; kanamayla kaybedilen demiri desteklemeye yardımcı olabilir."),
            Item(CyclePhase.Menstrual, ContentType.FoodDo, "Somon",
                "Omega-3 içerir; bazı kişilerde kramp şiddetini azalttığı bildirilmiştir."),
            Item(CyclePhase.Menstrual, ContentType.FoodDo, "Ceviz",
                "Magnezyum kaynağı; kas gevşemesine katkı sağlayabilir."),
            Item(CyclePhase.Menstrual, ContentType.FoodAvoid, "Aşırı tuz",
                "Şişkinlik hissini artırabilir; sınırlı tüketmek rahatlatabilir."),
            Item(CyclePhase.Menstrual, ContentType.FoodAvoid, "Kafein",
                "Bazı kişilerde kramp ve huzursuzluğu artırabilir."),
            Item(CyclePhase.Menstrual, ContentType.ExerciseDo, "Yoga",
                "Nazik esneme, kas gerginliğini azaltmaya yardımcı olabilir."),
            Item(CyclePhase.Menstrual, ContentType.ExerciseDo, "Yürüyüş",
                "Hafif hareket, dolaşımı destekler ve zorlamaz."),
            Item(CyclePhase.Menstrual, ContentType.ExerciseAvoid, "HIIT",
                "Yüksek yoğunluk bu dönemde bazı kişilere ağır gelebilir."),

            // --- Foliküler ---
            Item(CyclePhase.Follicular, ContentType.FoodDo, "Yumurta",
                "Kaliteli protein; yükselen enerjiyi desteklemeye uygun."),
            Item(CyclePhase.Follicular, ContentType.FoodDo, "Avokado",
                "Sağlıklı yağlar içerir, hormon dengesi için faydalı yağ asitleri sağlar."),
            Item(CyclePhase.Follicular, ContentType.FoodDo, "Yeşil yapraklılar",
                "Folat ve lif açısından zengin."),
            Item(CyclePhase.Follicular, ContentType.FoodAvoid, "İşlenmiş şeker",
                "Enerji dalgalanmalarına yol açabilir; dengeli tüketmek daha iyi."),
            Item(CyclePhase.Follicular, ContentType.ExerciseDo, "Kardiyo",
                "Artan enerjiyi değerlendirmek için uygun bir dönem."),
            Item(CyclePhase.Follicular, ContentType.ExerciseDo, "Kuvvet antrenmanı",
                "Toparlanma kapasitesi bu fazda genelde daha iyidir."),
            Item(CyclePhase.Follicular, ContentType.ExerciseAvoid, "Uzun süreli hareketsizlik",
                "Enerjinin yüksek olduğu bu dönemde hareket daha iyi hissettirebilir."),

            // --- Ovulasyon ---
            Item(CyclePhase.Ovulation, ContentType.FoodDo, "Renkli sebzeler",
                "Antioksidan çeşitliliği sağlar."),
            Item(CyclePhase.Ovulation, ContentType.FoodDo, "Kabak çekirdeği",
                "Çinko kaynağı."),
            Item(CyclePhase.Ovulation, ContentType.FoodAvoid, "Alkol",
                "Uyku kalitesini ve ruh hâli dengesini bozabilir."),
            Item(CyclePhase.Ovulation, ContentType.ExerciseDo, "Yüksek yoğunluklu antrenman",
                "Birçok kişi bu günlerde kendini en güçlü hisseder."),
            Item(CyclePhase.Ovulation, ContentType.ExerciseDo, "Grup sporları",
                "Sosyal enerjinin yüksek olduğu bir dönem."),
            Item(CyclePhase.Ovulation, ContentType.ExerciseAvoid, "Aşırı zorlanma",
                "Kendini iyi hissetmek, sınırları zorlamak anlamına gelmiyor."),

            // --- Luteal ---
            Item(CyclePhase.Luteal, ContentType.FoodDo, "Tatlı patates",
                "Kompleks karbonhidrat; kan şekerini daha dengeli tutmaya yardımcı olabilir."),
            Item(CyclePhase.Luteal, ContentType.FoodDo, "Bitter çikolata",
                "Magnezyum içerir; tatlı isteğine daha dengeli bir seçenek."),
            Item(CyclePhase.Luteal, ContentType.FoodDo, "Mercimek",
                "Lif ve bitkisel protein; tokluk hissini destekler."),
            Item(CyclePhase.Luteal, ContentType.FoodAvoid, "Aşırı kafein",
                "Kaygı ve göğüs hassasiyetini artırabilir."),
            Item(CyclePhase.Luteal, ContentType.FoodAvoid, "Çok tuzlu atıştırmalıklar",
                "Su tutulumunu ve şişkinliği artırabilir."),
            Item(CyclePhase.Luteal, ContentType.ExerciseDo, "Pilates",
                "Orta yoğunluk, güç ve esneklik dengesi sunar."),
            Item(CyclePhase.Luteal, ContentType.ExerciseDo, "Hafif kardiyo",
                "Endorfin salınımı ruh hâline iyi gelebilir."),
            Item(CyclePhase.Luteal, ContentType.ExerciseAvoid, "Ağır kaldırma",
                "Toparlanma bu fazda yavaşlayabilir; zorlanma riski artar."));

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Faz içerikleri eklendi.");
    }

    private static ContentItem Item(
        CyclePhase phase, ContentType type, string title, string body) =>
        new() { Phase = phase, Type = type, Title = title, Body = body };
}
