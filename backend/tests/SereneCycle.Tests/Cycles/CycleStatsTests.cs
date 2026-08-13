using SereneCycle.Domain.Cycles;

namespace SereneCycle.Tests.Cycles;

public class CycleStatsTests
{
    private const int OnboardingFallback = 28;

    [Fact]
    public void FewerThanThreeCycles_ReturnsOnboardingFallback()
    {
        // 1-2 döngü, kullanıcının kendi beyanından daha iyi bir tahmin değil.
        Assert.Equal(
            OnboardingFallback,
            CycleStats.RecomputeAverageCycleLength([], OnboardingFallback));

        Assert.Equal(
            OnboardingFallback,
            CycleStats.RecomputeAverageCycleLength([30], OnboardingFallback));

        Assert.Equal(
            OnboardingFallback,
            CycleStats.RecomputeAverageCycleLength([30, 31], OnboardingFallback));
    }

    [Fact]
    public void ThreeOrMoreCycles_ReturnsRollingAverage()
    {
        // (30 + 31 + 29) / 3 = 30
        Assert.Equal(
            30,
            CycleStats.RecomputeAverageCycleLength(
                [30, 31, 29], OnboardingFallback));
    }

    [Fact]
    public void OnlyMostRecentCyclesWithinWindowCount()
    {
        // En yeni 6 döngü: hepsi 30 → ortalama 30. Penceredeki 7. ve sonrası
        // (uçuk 60'lar) hesaba katılmamalı.
        int[] lengths = [30, 30, 30, 30, 30, 30, 60, 60, 60];

        Assert.Equal(
            30,
            CycleStats.RecomputeAverageCycleLength(lengths, OnboardingFallback));
    }

    [Fact]
    public void CustomWindow_IsRespected()
    {
        // Pencere 3 → sadece [28, 28, 28] → 28
        int[] lengths = [28, 28, 28, 40, 40, 40];

        Assert.Equal(
            28,
            CycleStats.RecomputeAverageCycleLength(
                lengths, OnboardingFallback, window: 3));
    }

    [Fact]
    public void AverageIsRoundedToNearestDay()
    {
        // (28 + 29 + 30) / 3 = 29
        Assert.Equal(
            29,
            CycleStats.RecomputeAverageCycleLength(
                [28, 29, 30], OnboardingFallback));

        // (28 + 29 + 30 + 30) / 4 = 29.25 → 29
        Assert.Equal(
            29,
            CycleStats.RecomputeAverageCycleLength(
                [28, 29, 30, 30], OnboardingFallback));

        // (29 + 30 + 30) / 3 = 29.67 → 30
        Assert.Equal(
            30,
            CycleStats.RecomputeAverageCycleLength(
                [29, 30, 30], OnboardingFallback));
    }

    [Fact]
    public void NewCycleShiftsWindowAndChangesPrediction()
    {
        // Kullanıcının döngüsü uzamaya başlıyor: yeni döngü eklendikçe
        // ortalama ve dolayısıyla tahmin kayıyor.
        int[] before = [28, 28, 28];
        var averageBefore =
            CycleStats.RecomputeAverageCycleLength(before, OnboardingFallback);

        int[] after = [34, 28, 28, 28];
        var averageAfter =
            CycleStats.RecomputeAverageCycleLength(after, OnboardingFallback);

        Assert.Equal(28, averageBefore);
        Assert.Equal(30, averageAfter); // (34+28+28+28)/4 = 29.5 → 30

        var start = new DateOnly(2026, 1, 1);
        var predictionBefore = PhaseCalculator.Calculate(
            start, averageBefore, 5, start);
        var predictionAfter = PhaseCalculator.Calculate(
            start, averageAfter, 5, start);

        Assert.Equal(
            start.AddDays(28), predictionBefore.PredictedNextPeriod);
        Assert.Equal(
            start.AddDays(30), predictionAfter.PredictedNextPeriod);
    }
}
