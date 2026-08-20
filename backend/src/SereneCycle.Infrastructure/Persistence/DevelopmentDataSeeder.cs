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
/// Üretimde asla çağrılmaz (Program.cs ortama göre karar verir) ve
/// yapılandırmadan kapatılabilir.
///
/// Faz içerik kataloğu buraya <b>dahil değil</b>: o örnek veri değil,
/// uygulamanın çalışması için gereken referans veridir ve
/// <see cref="ContentCatalogSeeder"/> tarafından her açılışta hizalanır.
/// </summary>
public static class DevelopmentDataSeeder
{
    public const string TestUserEmail = "test@serenecycle.app";
    public const string TestUserPassword = "Test1234!";

    // Örnek günlüklere bağlanan semptomlar; adlar SymptomSeedData ile birebir.
    private const string CrampsSymptom = "Karın krampları";
    private const string TiredSymptom = "Yorgunluk";
    private const string EnergeticSymptom = "Enerjik";

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

        // Üç ayrı gidiş-dönüş yerine tek sorgu: örnek kayıtların bağlanacağı
        // semptomların id'leri bir kerede okunur.
        string[] names = [CrampsSymptom, TiredSymptom, EnergeticSymptom];

        var symptomIdsByName = await db.Symptoms
            .AsNoTracking()
            .Where(s => names.Contains(s.Name))
            .ToDictionaryAsync(s => s.Name, s => s.Id, cancellationToken);

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

        var links = new List<LogSymptom>(names.Length);

        Link(firstDay, CrampsSymptom);
        Link(firstDay, TiredSymptom);
        Link(yesterday, EnergeticSymptom);

        void Link(DailyLog log, string symptomName)
        {
            if (symptomIdsByName.TryGetValue(symptomName, out var symptomId))
            {
                links.Add(new LogSymptom
                {
                    LogId = log.Id, SymptomId = symptomId
                });
            }
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
}
