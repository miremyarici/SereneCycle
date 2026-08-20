namespace SereneCycle.Domain.Cycles;

/// <summary>
/// Tamamlanmış döngülerden ortalama döngü uzunluğunu türetir.
/// <see cref="PhaseCalculator"/> gibi saf tutulmuştur.
/// </summary>
public static class CycleStats
{
    /// <summary>Kayan ortalamanın güvenilir sayılması için gereken en az döngü sayısı.</summary>
    public const int MinCyclesForAverage = 3;

    /// <summary>Ortalamaya dahil edilecek en fazla (en yeni) döngü sayısı.</summary>
    public const int DefaultWindow = 6;

    /// <param name="completedCycleLengthsNewestFirst">
    /// Tamamlanmış döngü uzunlukları, en yenisi başta olacak şekilde.
    /// </param>
    /// <param name="fallback">
    /// Yeterli veri yoksa dönecek değer — onboarding'de girilen tahmin.
    /// </param>
    /// <param name="window">Ortalamaya alınacak en yeni döngü sayısı.</param>
    public static int RecomputeAverageCycleLength(
        IReadOnlyList<int> completedCycleLengthsNewestFirst,
        int fallback,
        int window = DefaultWindow)
    {
        ArgumentNullException.ThrowIfNull(completedCycleLengthsNewestFirst);

        // 1-2 döngü, kullanıcının kendi beyanından daha iyi bir tahmin değil.
        if (completedCycleLengthsNewestFirst.Count < MinCyclesForAverage)
        {
            return fallback;
        }

        // Tam sayı gün döndürüyoruz; en yakın güne yuvarlanır. Araya liste
        // konmuyor: tek geçiş yeterli, ara materyalizasyon gereksiz.
        var average = completedCycleLengthsNewestFirst
            .Take(window)
            .Average();

        return (int)Math.Round(average, MidpointRounding.AwayFromZero);
    }
}
