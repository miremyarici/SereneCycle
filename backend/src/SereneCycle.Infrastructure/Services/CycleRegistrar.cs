using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SereneCycle.Application.Common;
using SereneCycle.Domain.Cycles;
using SereneCycle.Domain.Entities;
using SereneCycle.Infrastructure.Identity;
using SereneCycle.Infrastructure.Persistence;

namespace SereneCycle.Infrastructure.Services;

/// <summary>
/// Yeni adet başlangıcı kaydetmenin tek yeri. Hem döngü ayarları ekranı
/// (tarih seçimi) hem de adet kaydı ekranı (kanama işaretleme) buradan
/// geçer ki ortalama yeniden hesaplama mantığı ikiye ayrılmasın.
/// </summary>
public class CycleRegistrar(
    AppDbContext db,
    UserManager<AppUser> userManager,
    TimeProvider timeProvider,
    RiskSummaryUpdater riskSummaryUpdater)
{
    /// <summary>
    /// Açık döngüyü kapatır, verilen tarihte yenisini açar ve ortalama
    /// döngü uzunluğunu yeniden hesaplar. Aynı tarihte kayıt varsa
    /// hiçbir şey yapmaz.
    /// </summary>
    public async Task<Result> StartCycleAsync(
        AppUser user,
        DateOnly startDate,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        if (startDate > today)
        {
            return Result.Failure(
                ErrorCode.Validation,
                "Adet başlangıcı gelecek bir tarih olamaz.");
        }

        var alreadyExists = await db.Cycles.AnyAsync(
            c => c.UserId == user.Id && c.StartDate == startDate,
            cancellationToken);

        if (alreadyExists)
        {
            return Result.Success();
        }

        var previous = await db.Cycles
            .Where(c => c.UserId == user.Id && c.StartDate < startDate)
            .OrderByDescending(c => c.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (previous is not null)
        {
            previous.EndDate = startDate;
            previous.UpdatedAt = DateTimeOffset.UtcNow;
        }

        var opened = new Cycle
        {
            UserId = user.Id,
            StartDate = startDate
        };

        db.Cycles.Add(opened);

        await db.SaveChangesAsync(cancellationToken);
        await RecomputeAverageCycleLengthAsync(user, cancellationToken);

        // Döngü sınırları değişti: kapanan döngünün günleri artık yeni
        // döngüye ait olabilir, iki özet de yeniden hesaplanmalı.
        if (previous is not null)
        {
            await riskSummaryUpdater.RecomputeAsync(previous, cancellationToken);
        }

        await riskSummaryUpdater.RecomputeAsync(opened, cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// Kanama işaretlenen bir günün yeni bir döngü başlatıp başlatmadığına
    /// karar verir: süregelen bir adetin ortasındaysa (bir önceki güne de
    /// kanama girilmişse ya da açık döngü bu günü kapsıyorsa) yeni döngü
    /// açılmaz.
    /// </summary>
    public async Task<Result> RegisterBleedingDayAsync(
        AppUser user,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        // Gelecekteki bir güne kanama girilebilir (kullanıcı öngörüyor
        // olabilir) ama bu döngü geçmişini değiştirmemeli.
        if (date > today)
        {
            return Result.Success();
        }

        var periodWindowStart = date.AddDays(-(user.AvgPeriodLength - 1));

        var startedRecently = await db.Cycles.AnyAsync(
            c => c.UserId == user.Id
                 && c.StartDate <= date
                 && c.StartDate >= periodWindowStart,
            cancellationToken);

        if (startedRecently)
        {
            return Result.Success();
        }

        var previousDayBled = await db.DailyLogs.AnyAsync(
            l => l.UserId == user.Id
                 && l.LogDate == date.AddDays(-1)
                 && l.HasBleeding,
            cancellationToken);

        return previousDayBled
            ? Result.Success()
            : await StartCycleAsync(user, date, cancellationToken);
    }

    private async Task RecomputeAverageCycleLengthAsync(
        AppUser user,
        CancellationToken cancellationToken)
    {
        var completed = await db.Cycles
            .Where(c => c.UserId == user.Id && c.EndDate != null)
            .OrderByDescending(c => c.StartDate)
            .Take(CycleStats.DefaultWindow)
            .Select(c => c.EndDate!.Value.DayNumber - c.StartDate.DayNumber)
            .ToListAsync(cancellationToken);

        var recomputed = CycleStats.RecomputeAverageCycleLength(
            completed, fallback: user.AvgCycleLength);

        if (recomputed == user.AvgCycleLength)
        {
            return;
        }

        user.AvgCycleLength = recomputed;
        await userManager.UpdateAsync(user);
    }
}
