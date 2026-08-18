using SereneCycle.Domain.Cycles;

namespace SereneCycle.Domain.Risk;

/// <summary>
/// Kuralların eşikleri tek yerde. Hepsi yaygın klinik "takip etmeye değer"
/// sınırlarından alındı; teşhis eşiği değil, kullanıcıyı kendi verisine
/// bakmaya davet eden sınırlar.
/// </summary>
public static class RiskThresholds
{
    /// <summary>Bu kadar (ve daha fazla) ardışık kanama günü uzun sayılır.</summary>
    public const int ProlongedBleedingDays = 8;

    /// <summary>Üst üste bu kadar "çok" şiddetinde gün dikkat çeker.</summary>
    public const int HeavyStreakDays = 3;

    /// <summary>Tamamlanmış döngüde bu kadar (veya daha az) kanama günü kısa sayılır.</summary>
    public const int VeryShortPeriodMaxDays = 1;

    /// <summary>
    /// Bu gün numarasından itibaren görülen lekelenme "adet dışı" sayılır.
    /// Sabit tutuldu: kullanıcının ortalama adet süresi sonradan değişince
    /// saklanmış özetin anlamı kaymasın.
    /// </summary>
    public const int SpottingOutsidePeriodFromDay = 8;

    /// <summary>Adet dışı lekelenmenin işaret üretmesi için gereken gün sayısı.</summary>
    public const int SpottingOutsidePeriodDays = 3;

    /// <summary>
    /// İki kanama günü arasında bu kadardan fazla gün varsa kanama
    /// "yeniden başlamış" sayılır (yani arada en az iki kanamasız gün var).
    /// </summary>
    public const int BleedingRestartGapDays = 2;

    /// <summary>Yeni adet başlamadan geçen gün sayısı bunu aşarsa işaret üretilir.</summary>
    public const int ProlongedAbsenceDays = 90;

    /// <summary>Döngü içinde bu kadar ağrılı gün sık sayılır.</summary>
    public const int FrequentPainDays = 5;
}

/// <summary>
/// Kuralların günlerden çıkmayan girdileri: döngünün kapanıp kapanmadığı,
/// bugüne kadar geçen gün sayısı ve kişisel sapma sonucu.
/// </summary>
/// <param name="IsCycleComplete">
/// Döngü kapandı mı (yeni adet başladı mı). Kapanmamış döngüde "kanama çok
/// kısa sürdü" demek anlamsız: adet hâlâ sürüyor olabilir.
/// </param>
/// <param name="CycleDay">Döngü başlangıcından bugüne kadarki gün (1'den başlar).</param>
/// <param name="LastCycleDeviation">
/// Son tamamlanmış döngünün kişisel ortalamadan sapması; yeterli geçmiş
/// yoksa null.
/// </param>
public sealed record RiskContext(
    bool IsCycleComplete,
    int CycleDay,
    CycleLengthDeviationResult? LastCycleDeviation);

/// <summary>
/// Özet + bağlamı işaretlere çeviren saf kural motoru.
/// <see cref="PhaseCalculator"/> gibi hiçbir I/O ve zaman bağımlılığı yok.
/// </summary>
public static class RiskEvaluator
{
    public static RiskAssessment Evaluate(
        CycleRiskSignals signals,
        RiskContext context)
    {
        ArgumentNullException.ThrowIfNull(signals);
        ArgumentNullException.ThrowIfNull(context);

        var flags = new List<RiskFlag>();

        if (signals.LongestBleedingStreak
            >= RiskThresholds.ProlongedBleedingDays)
        {
            flags.Add(new RiskFlag(
                RiskFlagCode.ProlongedBleeding,
                RiskLevel.Attention,
                signals.LongestBleedingStreak));
        }

        if (signals.LongestHeavyStreak >= RiskThresholds.HeavyStreakDays)
        {
            flags.Add(new RiskFlag(
                RiskFlagCode.HeavyBleeding,
                RiskLevel.Attention,
                signals.LongestHeavyStreak));
        }

        if (signals.BleedingRestartCount > 0)
        {
            flags.Add(new RiskFlag(
                RiskFlagCode.IntermenstrualBleeding,
                RiskLevel.Attention,
                signals.BleedingRestartCount));
        }

        if ((signals.BloodColorMask & BloodColorMasks.Unusual) != 0)
        {
            flags.Add(new RiskFlag(
                RiskFlagCode.UnusualBloodColor,
                RiskLevel.Attention));
        }

        // Amenore sinyali yalnızca hâlâ açık olan döngüde anlamlı.
        if (!context.IsCycleComplete
            && context.CycleDay > RiskThresholds.ProlongedAbsenceDays)
        {
            flags.Add(new RiskFlag(
                RiskFlagCode.ProlongedAbsence,
                RiskLevel.Attention,
                context.CycleDay));
        }

        // Kanama hiç kaydedilmemişse "çok kısa" demek veri eksikliğini
        // bulguya çevirmek olurdu; en az bir gün şart.
        if (context.IsCycleComplete
            && signals.BleedingDays > 0
            && signals.BleedingDays <= RiskThresholds.VeryShortPeriodMaxDays)
        {
            flags.Add(new RiskFlag(
                RiskFlagCode.VeryShortPeriod,
                RiskLevel.Info,
                signals.BleedingDays));
        }

        if (signals.SpottingOutsidePeriodDays
            >= RiskThresholds.SpottingOutsidePeriodDays)
        {
            flags.Add(new RiskFlag(
                RiskFlagCode.MidCycleSpotting,
                RiskLevel.Info,
                signals.SpottingOutsidePeriodDays));
        }

        if (context.LastCycleDeviation is { IsNotable: true } deviation)
        {
            flags.Add(new RiskFlag(
                RiskFlagCode.CycleLengthDeviation,
                RiskLevel.Info,
                deviation.DeltaDays,
                deviation.BaselineAverage));
        }

        if (signals.PainDays >= RiskThresholds.FrequentPainDays)
        {
            flags.Add(new RiskFlag(
                RiskFlagCode.FrequentPain,
                RiskLevel.Info,
                signals.PainDays));
        }

        if (flags.Count == 0)
        {
            return RiskAssessment.Empty;
        }

        // Ağır işaretler üstte; eşitlikte ekleme sırası korunur (OrderBy
        // kararlıdır), yani kural sırası aynı zamanda gösterim sırası.
        var ordered = flags
            .OrderByDescending(f => f.Level)
            .ToList();

        return new RiskAssessment(ordered.Max(f => f.Level), ordered);
    }
}
