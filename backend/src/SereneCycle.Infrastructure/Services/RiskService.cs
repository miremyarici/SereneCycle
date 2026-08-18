using Microsoft.EntityFrameworkCore;
using SereneCycle.Application.Risk;
using SereneCycle.Domain.Cycles;
using SereneCycle.Domain.Entities;
using SereneCycle.Domain.Risk;
using SereneCycle.Infrastructure.Persistence;

namespace SereneCycle.Infrastructure.Services;

/// <summary>
/// Risk kartının okuma yolu: özet satırını getirir, kişisel sapmayı
/// hesaplar ve saf kural motorunu çalıştırır. Kullanıcının toplam geçmiş
/// uzunluğu hiçbir sorguya girmez — en fazla son 7 döngü okunur.
/// </summary>
public class RiskService(AppDbContext db, RiskSummaryUpdater updater)
    : IRiskService
{
    public async Task<RiskCard> GetCardAsync(
        Cycle cycle,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cycle);

        var summary = await db.CycleRiskSummaries
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.CycleId == cycle.Id, cancellationToken);

        // Özet normalde kayıt yazılırken üretilir. Yoksa (risk motorundan
        // önce açılmış bir döngü) bir kez burada hesaplanır ve saklanır;
        // sonraki okumalar yine tek satır getirisine düşer.
        summary ??= await FillMissingSummaryAsync(cycle, cancellationToken);

        var signals = summary.ToSignals();

        var context = new RiskContext(
            IsCycleComplete: cycle.EndDate is not null,
            CycleDay: Math.Max(
                1, today.DayNumber - cycle.StartDate.DayNumber + 1),
            LastCycleDeviation:
                await EvaluateDeviationAsync(cycle, cancellationToken));

        var assessment = RiskEvaluator.Evaluate(signals, context);

        return new RiskCard(
            Level: assessment.Level,
            Title: RiskContent.CardTitleOf(assessment.Level),
            Message: RiskContent.CardMessageOf(
                assessment.Level, signals.LoggedDays),
            Flags: [.. assessment.Flags.Select(f => new RiskFlagView(
                Code: f.Code,
                Level: f.Level,
                Title: RiskContent.TitleOf(f.Code),
                Detail: RiskContent.DetailOf(f)))],
            Stats: new RiskCardStats(
                LoggedDays: signals.LoggedDays,
                BleedingDays: signals.BleedingDays,
                SpottingDays: signals.SpottingDays,
                PainDays: signals.PainDays),
            Disclaimer: RiskContent.Disclaimer);
    }

    /// <summary>
    /// Eksik özeti bir kez hesaplayıp saklar. Ana sayfa aynı anda iki kez
    /// açılırsa (yenile + ilk yükleme) iki istek de yazmayı deneyebilir;
    /// birincil anahtar çakışması bir hata değil, "diğeri yazdı" demektir.
    /// </summary>
    private async Task<CycleRiskSummary> FillMissingSummaryAsync(
        Cycle cycle,
        CancellationToken cancellationToken)
    {
        try
        {
            return await updater.RecomputeAsync(cycle, cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Yazılamayan satır izlemede kalmasın: aynı istek içindeki
            // sonraki SaveChanges çağrıları da onunla birlikte patlardı.
            foreach (var entry in db.ChangeTracker
                         .Entries<CycleRiskSummary>()
                         .Where(e => e.State == EntityState.Added)
                         .ToList())
            {
                entry.State = EntityState.Detached;
            }

            return await db.CycleRiskSummaries
                .AsNoTracking()
                .FirstAsync(s => s.CycleId == cycle.Id, cancellationToken);
        }
    }

    /// <summary>
    /// Son tamamlanmış döngünün, ondan önceki (en fazla 6) döngünün
    /// ortalamasından sapması. Sorgu sabit sayıda satırla sınırlı: geçmiş
    /// büyüdükçe maliyet artmaz.
    /// </summary>
    private async Task<CycleLengthDeviationResult?> EvaluateDeviationAsync(
        Cycle cycle,
        CancellationToken cancellationToken)
    {
        var previous = await db.Cycles
            .Where(c => c.UserId == cycle.UserId
                        && c.EndDate != null
                        && c.StartDate < cycle.StartDate)
            .OrderByDescending(c => c.StartDate)
            .Take(CycleStats.DefaultWindow + 1)
            .Select(c => c.EndDate!.Value.DayNumber - c.StartDate.DayNumber)
            .ToListAsync(cancellationToken);

        // Kartın döngüsü kapanmışsa karşılaştırılacak "son döngü" kendisidir;
        // açıksa henüz uzunluğu bilinmediği için bir öncekine bakılır.
        if (cycle.LengthInDays is { } ownLength)
        {
            return CycleLengthDeviation.Evaluate(ownLength, previous);
        }

        return previous.Count == 0
            ? null
            : CycleLengthDeviation.Evaluate(previous[0], previous[1..]);
    }
}
