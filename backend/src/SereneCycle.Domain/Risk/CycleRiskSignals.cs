using SereneCycle.Domain.Entities;

namespace SereneCycle.Domain.Risk;

/// <summary>
/// Semptom id'lerini 64 bitlik bir maskeye indirger: id 1 → bit 0.
/// Böylece "bu günde ağrı var mı" sorusu bir <c>AND</c> komutuna,
/// "bu döngüde hangi semptomlar görüldü" sorusu bir <c>OR</c> birikimine
/// düşer; semptom sayısı üzerinde döngü kurmaya gerek kalmaz.
/// </summary>
public static class SymptomMasks
{
    /// <summary>Maskeye sığan en büyük semptom id'si.</summary>
    public const int MaxSymptomId = 64;

    /// <summary>Kramp, bel ve pelvis ağrısı — "ağrılı gün" tanımı.</summary>
    public static readonly long Pain = Of([1, 7, 17]);

    public static long BitOf(int symptomId) =>
        symptomId is >= 1 and <= MaxSymptomId
            ? 1L << (symptomId - 1)
            : 0L;

    public static long Of(IEnumerable<int> symptomIds)
    {
        ArgumentNullException.ThrowIfNull(symptomIds);

        var mask = 0L;

        foreach (var id in symptomIds)
        {
            mask |= BitOf(id);
        }

        return mask;
    }

    public static bool Contains(long mask, int symptomId) =>
        (mask & BitOf(symptomId)) != 0;
}

/// <summary>Kan rengi enum'unun bit maskesi karşılığı (6 bit yeter).</summary>
public static class BloodColorMasks
{
    /// <summary>
    /// Bazen enfeksiyon belirtisi olarak anılan tonlar. Kesin bir gösterge
    /// değil; kartta yalnızca "kaydettiğin renk dikkat çekici" denir.
    /// </summary>
    public static readonly int Unusual =
        BitOf(BloodColor.Gray) | BitOf(BloodColor.Orange);

    public static int BitOf(BloodColor color) => 1 << (int)color;

    public static bool Contains(int mask, BloodColor color) =>
        (mask & BitOf(color)) != 0;
}

/// <summary>
/// Risk motoruna verilen tek gün. <paramref name="DayIndex"/> döngü içindeki
/// gün numarasıdır (adetin ilk günü = 1); kayıt tutulmayan günler hiç
/// gelmediği için ardışıklık tarih yerine bu numaradan anlaşılır.
/// </summary>
public readonly record struct RiskDay(
    int DayIndex,
    bool HasBleeding,
    FlowIntensity? Flow,
    BloodColor? BloodColor,
    bool HasSpotting,
    long SymptomMask);

/// <summary>
/// Bir döngünün risk kuralları için gereken özeti. Hepsi sabit alan: gün
/// sayısı ne olursa olsun bu kayıt aynı boyutta kalır, bu yüzden döngü
/// başına tek satır olarak saklanabilir.
/// </summary>
public sealed record CycleRiskSignals(
    int LoggedDays,
    int BleedingDays,
    int LongestBleedingStreak,
    int HeavyDays,
    int LongestHeavyStreak,
    int SpottingDays,
    int SpottingOutsidePeriodDays,
    int BleedingRestartCount,
    int PainDays,
    int BloodColorMask,
    long SymptomMask)
{
    public static readonly CycleRiskSignals Empty =
        new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0L);

    /// <summary>Hiç kayıt yoksa kart "veri yok" durumuna geçer.</summary>
    public bool HasAnyLog => LoggedDays > 0;
}

/// <summary>
/// Günleri tarih sırasıyla tek geçişte özetler. Bütün sayaçlar koşan
/// sayaç olduğu için geçmişi bellekte tutmaya gerek yok: zaman O(d),
/// alan O(1).
///
/// Günler artan <see cref="RiskDay.DayIndex"/> sırasıyla verilmelidir.
/// </summary>
public sealed class CycleRiskAccumulator
{
    private int _loggedDays;
    private int _bleedingDays;
    private int _bleedingStreak;
    private int _longestBleedingStreak;
    private int _heavyDays;
    private int _heavyStreak;
    private int _longestHeavyStreak;
    private int _spottingDays;
    private int _spottingOutsidePeriodDays;
    private int _bleedingRestartCount;
    private int _painDays;
    private int _bloodColorMask;
    private long _symptomMask;

    private int _lastBleedingDayIndex;
    private int _lastHeavyDayIndex;

    public void Add(RiskDay day)
    {
        _loggedDays++;

        _symptomMask |= day.SymptomMask;

        if ((day.SymptomMask & SymptomMasks.Pain) != 0)
        {
            _painDays++;
        }

        if (day.HasSpotting)
        {
            _spottingDays++;

            if (day.DayIndex >= RiskThresholds.SpottingOutsidePeriodFromDay)
            {
                _spottingOutsidePeriodDays++;
            }
        }

        if (!day.HasBleeding)
        {
            return;
        }

        _bleedingDays++;

        // Kanama bittikten sonra yeniden başladıysa (arada en az bir tam
        // kanamasız gün varsa) bu adet arası kanama sayılır.
        if (_lastBleedingDayIndex > 0
            && day.DayIndex - _lastBleedingDayIndex
                > RiskThresholds.BleedingRestartGapDays)
        {
            _bleedingRestartCount++;
        }

        _bleedingStreak = day.DayIndex == _lastBleedingDayIndex + 1
            ? _bleedingStreak + 1
            : 1;
        _longestBleedingStreak =
            Math.Max(_longestBleedingStreak, _bleedingStreak);
        _lastBleedingDayIndex = day.DayIndex;

        if (day.BloodColor is { } color)
        {
            _bloodColorMask |= BloodColorMasks.BitOf(color);
        }

        if (day.Flow != FlowIntensity.Heavy)
        {
            return;
        }

        _heavyDays++;
        _heavyStreak = day.DayIndex == _lastHeavyDayIndex + 1
            ? _heavyStreak + 1
            : 1;
        _longestHeavyStreak = Math.Max(_longestHeavyStreak, _heavyStreak);
        _lastHeavyDayIndex = day.DayIndex;
    }

    public CycleRiskSignals Build() => new(
        LoggedDays: _loggedDays,
        BleedingDays: _bleedingDays,
        LongestBleedingStreak: _longestBleedingStreak,
        HeavyDays: _heavyDays,
        LongestHeavyStreak: _longestHeavyStreak,
        SpottingDays: _spottingDays,
        SpottingOutsidePeriodDays: _spottingOutsidePeriodDays,
        BleedingRestartCount: _bleedingRestartCount,
        PainDays: _painDays,
        BloodColorMask: _bloodColorMask,
        SymptomMask: _symptomMask);
}
