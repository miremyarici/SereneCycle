namespace SereneCycle.Domain.Cycles;

/// <summary>
/// Döngü fazı hesaplamasının tek kaynağı. Bilinçli olarak saf (pure) tutuldu:
/// hiçbir I/O, DateTime.Now veya bağımlılık içermez, böylece bütün sınır
/// durumları deterministik olarak test edilebilir.
///
/// Mobil taraftaki Dart karşılığı (mobile/lib/core/domain/cycle/) ile aynı
/// mantığı uygular; iki tarafın da aynı sonucu vermesi testlerle korunur.
/// </summary>
public static class PhaseCalculator
{
    /// <summary>Luteal faz uzunluğu kişiden kişiye az değişir; sabit kabul edilir.</summary>
    public const int LutealPhaseLength = 14;

    /// <summary>Bu aralığın dışındaki döngüler "düzensiz" sayılır.</summary>
    public const int MinRegularCycleLength = 21;
    public const int MaxRegularCycleLength = 35;

    /// <param name="lastPeriodStart">Son adetin ilk günü (S).</param>
    /// <param name="avgCycleLength">Ortalama döngü uzunluğu (C).</param>
    /// <param name="avgPeriodLength">Ortalama adet süresi (P).</param>
    /// <param name="today">
    /// "Bugün" dışarıdan verilir — testlerin deterministik olması için kritik.
    /// </param>
    public static PhasePrediction Calculate(
        DateOnly lastPeriodStart,
        int avgCycleLength,
        int avgPeriodLength,
        DateOnly today)
    {
        if (avgCycleLength < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(avgCycleLength),
                avgCycleLength,
                "Döngü uzunluğu en az 1 gün olmalı.");
        }

        if (avgPeriodLength < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(avgPeriodLength),
                avgPeriodLength,
                "Adet süresi en az 1 gün olmalı.");
        }

        if (today < lastPeriodStart)
        {
            throw new ArgumentOutOfRangeException(
                nameof(today),
                today,
                "Bugün, son adet başlangıcından önce olamaz.");
        }

        // Döngü içindeki gün, 1'den başlar: adetin ilk günü = 1. gün.
        var cycleDay = today.DayNumber - lastPeriodStart.DayNumber + 1;

        var predictedNextPeriod = lastPeriodStart.AddDays(avgCycleLength);
        var predictedOvulation =
            predictedNextPeriod.AddDays(-LutealPhaseLength);

        // Ovulasyonun döngü içindeki gün numarası.
        var ovulationDay = avgCycleLength - LutealPhaseLength;

        var phase = DeterminePhase(cycleDay, avgPeriodLength, ovulationDay);

        var isIrregular = avgCycleLength < MinRegularCycleLength
                          || avgCycleLength > MaxRegularCycleLength;

        return new PhasePrediction(
            CurrentPhase: phase,
            CycleDay: cycleDay,
            CycleLength: avgCycleLength,
            PredictedNextPeriod: predictedNextPeriod,
            PredictedOvulation: predictedOvulation,
            IsIrregular: isIrregular,
            IsPeriodLate: cycleDay > avgCycleLength);
    }

    /// <summary>
    /// Faz sınırları:
    ///   Menstrüel : 1 .. P
    ///   Ovulasyon : ovulationDay-1 .. ovulationDay+1  (3 günlük pencere)
    ///   Foliküler : P+1 .. ovulationDay-2
    ///   Luteal    : ovulationDay+2 .. C
    ///
    /// Kontrol sırası bilinçli: orijinal tanımda "foliküler P+1..ovulasyon-1"
    /// ile "ovulasyon ±1" bir gün çakışıyordu. Çakışmada ovulasyon penceresi
    /// önceliklidir, bu yüzden ovulasyon menstrüelden hemen sonra sınanır.
    /// Bu sıralama ayrıca çok kısa döngülerde (foliküler fazın hiç oluşmadığı
    /// durum) doğru sonucu verir.
    /// </summary>
    private static CyclePhase DeterminePhase(
        int cycleDay,
        int avgPeriodLength,
        int ovulationDay)
    {
        if (cycleDay <= avgPeriodLength)
        {
            return CyclePhase.Menstrual;
        }

        if (cycleDay >= ovulationDay - 1 && cycleDay <= ovulationDay + 1)
        {
            return CyclePhase.Ovulation;
        }

        if (cycleDay < ovulationDay - 1)
        {
            return CyclePhase.Follicular;
        }

        // Ovulasyon penceresinden sonrası (adet geciktiyse de burada kalır).
        return CyclePhase.Luteal;
    }
}
