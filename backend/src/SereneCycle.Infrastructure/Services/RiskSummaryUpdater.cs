using Microsoft.EntityFrameworkCore;
using SereneCycle.Domain.Entities;
using SereneCycle.Domain.Risk;
using SereneCycle.Infrastructure.Persistence;

namespace SereneCycle.Infrastructure.Services;

/// <summary>
/// Bir döngünün risk özetini yeniden hesaplayıp saklamanın tek yeri.
/// Kayıt yazılınca (gün kaydı ya da döngü sınırı değişince) çağrılır.
///
/// Günler <c>AsAsyncEnumerable</c> ile akış halinde okunur: özet koşan
/// sayaçlardan oluştuğu için geçmişi bellekte tutmaya gerek yok — zaman
/// O(d), alan O(1). Semptomlar için join yok, <see cref="DailyLog.SymptomMask"/>
/// kolonu okunur.
/// </summary>
public class RiskSummaryUpdater(AppDbContext db)
{
    /// <summary>
    /// Verilen günün ait olduğu döngüyü yeniden hesaplar. Gün hiçbir
    /// döngünün içine düşmüyorsa (ilk döngüden önce) hiçbir şey yapmaz.
    /// </summary>
    public async Task RecomputeForDateAsync(
        Guid userId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var cycle = await db.Cycles
            .Where(c => c.UserId == userId && c.StartDate <= date)
            .OrderByDescending(c => c.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (cycle is null || (cycle.EndDate is { } end && date >= end))
        {
            return;
        }

        await RecomputeAsync(cycle, cancellationToken);
    }

    /// <summary>
    /// Döngüyü baştan hesaplar ve özet satırını üzerine yazar. Kullanıcı eski
    /// bir günü düzenleyebildiği için artımlı güncelleme yapılmıyor; döngü
    /// en fazla ~35 gün olduğundan bu pratikte sabit maliyet.
    /// </summary>
    public async Task<CycleRiskSummary> RecomputeAsync(
        Cycle cycle,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cycle);

        var signals = await ComputeSignalsAsync(cycle, cancellationToken);

        var summary = await db.CycleRiskSummaries
            .FirstOrDefaultAsync(s => s.CycleId == cycle.Id, cancellationToken);

        if (summary is null)
        {
            summary = new CycleRiskSummary
            {
                CycleId = cycle.Id,
                UserId = cycle.UserId
            };

            db.CycleRiskSummaries.Add(summary);
        }

        summary.CycleStartDate = cycle.StartDate;
        summary.Apply(signals);

        await db.SaveChangesAsync(cancellationToken);

        return summary;
    }

    private async Task<CycleRiskSignals> ComputeSignalsAsync(
        Cycle cycle,
        CancellationToken cancellationToken)
    {
        var start = cycle.StartDate;

        var days = db.DailyLogs
            .Where(l => l.UserId == cycle.UserId && l.LogDate >= start);

        // Kapanmış döngü, bir sonraki döngünün ilk gününe kadar (dahil değil).
        if (cycle.EndDate is { } endDate)
        {
            days = days.Where(l => l.LogDate < endDate);
        }

        var accumulator = new CycleRiskAccumulator();

        // Projeksiyon: kaydın tamamı değil, kuralların ihtiyacı olan alanlar.
        var stream = days
            .OrderBy(l => l.LogDate)
            .Select(l => new
            {
                l.LogDate,
                l.HasBleeding,
                l.Flow,
                l.BloodColor,
                l.HasSpotting,
                l.SymptomMask
            })
            .AsAsyncEnumerable();

        await foreach (var day in stream.WithCancellation(cancellationToken))
        {
            accumulator.Add(new RiskDay(
                DayIndex: day.LogDate.DayNumber - start.DayNumber + 1,
                HasBleeding: day.HasBleeding,
                Flow: day.Flow,
                BloodColor: day.BloodColor,
                HasSpotting: day.HasSpotting,
                SymptomMask: day.SymptomMask));
        }

        return accumulator.Build();
    }
}
