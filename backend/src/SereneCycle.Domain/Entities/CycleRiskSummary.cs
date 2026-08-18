using SereneCycle.Domain.Risk;

namespace SereneCycle.Domain.Entities;

/// <summary>
/// Bir döngünün risk özeti — döngü başına tek satır. Okuma anında değil,
/// kayıt yazılırken hesaplanır: ana sayfa açılışı tek birincil anahtar
/// getirisine (O(1)) düşer, maliyet seyrek olan yazmaya kayar.
///
/// Kullanıcı eski bir günü düzenleyebildiği için artımlı güncelleme yok;
/// ilgili döngü baştan hesaplanır. Döngü ≤ 35 gün olduğu için bu pratikte
/// sabit maliyet.
/// </summary>
public class CycleRiskSummary
{
    /// <summary>Birincil anahtar aynı zamanda döngüye yabancı anahtar.</summary>
    public Guid CycleId { get; set; }

    public Guid UserId { get; set; }

    /// <summary>Döngünün ilk günü — satırı okumadan bağlam kurmak için.</summary>
    public DateOnly CycleStartDate { get; set; }

    public int LoggedDays { get; set; }
    public int BleedingDays { get; set; }
    public int LongestBleedingStreak { get; set; }
    public int HeavyDays { get; set; }
    public int LongestHeavyStreak { get; set; }
    public int SpottingDays { get; set; }
    public int SpottingOutsidePeriodDays { get; set; }
    public int BleedingRestartCount { get; set; }
    public int PainDays { get; set; }

    /// <summary>Görülen kan renklerinin bit maskesi (6 bit yeter).</summary>
    public int BloodColorMask { get; set; }

    /// <summary>Döngü boyunca görülen semptomların birleşik maskesi.</summary>
    public long SymptomMask { get; set; }

    public DateTimeOffset ComputedAt { get; set; } = DateTimeOffset.UtcNow;

    public CycleRiskSignals ToSignals() => new(
        LoggedDays: LoggedDays,
        BleedingDays: BleedingDays,
        LongestBleedingStreak: LongestBleedingStreak,
        HeavyDays: HeavyDays,
        LongestHeavyStreak: LongestHeavyStreak,
        SpottingDays: SpottingDays,
        SpottingOutsidePeriodDays: SpottingOutsidePeriodDays,
        BleedingRestartCount: BleedingRestartCount,
        PainDays: PainDays,
        BloodColorMask: BloodColorMask,
        SymptomMask: SymptomMask);

    public void Apply(CycleRiskSignals signals)
    {
        ArgumentNullException.ThrowIfNull(signals);

        LoggedDays = signals.LoggedDays;
        BleedingDays = signals.BleedingDays;
        LongestBleedingStreak = signals.LongestBleedingStreak;
        HeavyDays = signals.HeavyDays;
        LongestHeavyStreak = signals.LongestHeavyStreak;
        SpottingDays = signals.SpottingDays;
        SpottingOutsidePeriodDays = signals.SpottingOutsidePeriodDays;
        BleedingRestartCount = signals.BleedingRestartCount;
        PainDays = signals.PainDays;
        BloodColorMask = signals.BloodColorMask;
        SymptomMask = signals.SymptomMask;
        ComputedAt = DateTimeOffset.UtcNow;
    }
}
