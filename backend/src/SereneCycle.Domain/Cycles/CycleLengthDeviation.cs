namespace SereneCycle.Domain.Cycles;

/// <param name="IsNotable">Sapma, kullanıcının kendi dalgalanmasını aşıyor mu.</param>
/// <param name="DeltaDays">
/// Son döngü − kişisel ortalama. Pozitif: döngü uzadı, negatif: kısaldı.
/// </param>
/// <param name="BaselineAverage">Karşılaştırma tabanı (gün, yuvarlanmış).</param>
/// <param name="BaselineCount">Tabana giren döngü sayısı.</param>
/// <param name="BaselineStdDev">Tabandaki standart sapma (gün).</param>
public sealed record CycleLengthDeviationResult(
    bool IsNotable,
    int DeltaDays,
    int BaselineAverage,
    int BaselineCount,
    double BaselineStdDev);

/// <summary>
/// "Senin normalinden sapma" testi. Bütün geçmişi tutup varyans hesaplamak
/// yerine sabit bir pencere (<see cref="CycleStats.DefaultWindow"/>) kullanır:
/// alan O(1), güncelleme O(1) ve eski veriyi kendiliğinden unutur — döngüler
/// yaşam boyunca değiştiği için bu bir avantaj.
///
/// <see cref="PhaseCalculator"/> gibi saf: I/O ve zaman bağımlılığı yok.
/// </summary>
public static class CycleLengthDeviation
{
    /// <summary>Tabanın anlamlı sayılması için gereken en az döngü sayısı.</summary>
    public const int MinBaselineCycles = CycleStats.MinCyclesForAverage;

    /// <summary>
    /// Bu günden küçük sapmalar, dalgalanma ne kadar dar olursa olsun
    /// bildirilmez: bir-iki günlük oynama normaldir.
    /// </summary>
    public const int MinNotableDeltaDays = 4;

    /// <summary>
    /// Kaç standart sapma "belirgin" sayılır. Düzenli bir kullanıcıda
    /// eşiği alt sınır belirler; dalgalı bir kullanıcıda sigma yükseldiği
    /// için eşik kendiliğinden genişler.
    /// </summary>
    public const double SigmaMultiplier = 2.0;

    /// <param name="latestLength">Son tamamlanmış döngünün uzunluğu (gün).</param>
    /// <param name="baselineNewestFirst">
    /// Ondan önceki tamamlanmış döngü uzunlukları, en yenisi başta.
    /// </param>
    /// <param name="window">Tabana alınacak en yeni döngü sayısı.</param>
    public static CycleLengthDeviationResult Evaluate(
        int latestLength,
        IReadOnlyList<int> baselineNewestFirst,
        int window = CycleStats.DefaultWindow)
    {
        ArgumentNullException.ThrowIfNull(baselineNewestFirst);

        // Tek geçiş, koşan toplamlar: pencere zaten küçük ama sabit alan
        // garantisi burada da korunuyor.
        var count = 0;
        double sum = 0;
        double sumOfSquares = 0;

        foreach (var length in baselineNewestFirst)
        {
            if (count == window)
            {
                break;
            }

            count++;
            sum += length;
            sumOfSquares += (double)length * length;
        }

        if (count < MinBaselineCycles)
        {
            return new CycleLengthDeviationResult(
                IsNotable: false,
                DeltaDays: 0,
                BaselineAverage: 0,
                BaselineCount: count,
                BaselineStdDev: 0);
        }

        var mean = sum / count;

        // Kayan nokta hatası varyansı eksiye düşürebilir; sıfıra kırpıyoruz.
        var variance = Math.Max(0, sumOfSquares / count - mean * mean);
        var stdDev = Math.Sqrt(variance);

        var baselineAverage = (int)Math.Round(mean, MidpointRounding.AwayFromZero);
        var delta = latestLength - baselineAverage;
        var threshold = Math.Max(MinNotableDeltaDays, SigmaMultiplier * stdDev);

        return new CycleLengthDeviationResult(
            IsNotable: Math.Abs(delta) >= threshold,
            DeltaDays: delta,
            BaselineAverage: baselineAverage,
            BaselineCount: count,
            BaselineStdDev: stdDev);
    }
}
