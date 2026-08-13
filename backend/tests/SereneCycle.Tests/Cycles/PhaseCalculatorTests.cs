using SereneCycle.Domain.Cycles;

namespace SereneCycle.Tests.Cycles;

public class PhaseCalculatorTests
{
    // Referans senaryo: 1 Ocak 2026'da başlayan, 28 günlük, 5 gün adetli döngü.
    // Ovulasyon günü = 28 - 14 = 14. Ovulasyon penceresi = 13..15.
    private static readonly DateOnly PeriodStart = new(2026, 1, 1);
    private const int CycleLength = 28;
    private const int PeriodLength = 5;

    private static PhasePrediction CalculateOnDay(int cycleDay) =>
        PhaseCalculator.Calculate(
            PeriodStart,
            CycleLength,
            PeriodLength,
            PeriodStart.AddDays(cycleDay - 1));

    [Theory]
    [InlineData(1, CyclePhase.Menstrual)]
    [InlineData(3, CyclePhase.Menstrual)]
    [InlineData(8, CyclePhase.Follicular)]
    [InlineData(14, CyclePhase.Ovulation)]
    [InlineData(20, CyclePhase.Luteal)]
    public void NormalCycle_ReturnsExpectedPhase(int cycleDay, CyclePhase expected)
    {
        var result = CalculateOnDay(cycleDay);

        Assert.Equal(expected, result.CurrentPhase);
        Assert.Equal(cycleDay, result.CycleDay);
    }

    [Fact]
    public void NormalCycle_PredictsNextPeriodAndOvulation()
    {
        var result = CalculateOnDay(1);

        Assert.Equal(PeriodStart.AddDays(28), result.PredictedNextPeriod);
        Assert.Equal(PeriodStart.AddDays(14), result.PredictedOvulation);
        Assert.False(result.IsIrregular);
        Assert.False(result.IsPeriodLate);
    }

    // --- Sınır durumları -------------------------------------------------
    // Beklenen sınırlar: menstrüel 1..5, foliküler 6..12,
    // ovulasyon 13..15, luteal 16..28.

    [Theory]
    [InlineData(5, CyclePhase.Menstrual)]   // son menstrüel gün (P)
    [InlineData(6, CyclePhase.Follicular)]  // ilk foliküler gün (P+1)
    [InlineData(12, CyclePhase.Follicular)] // son foliküler gün (ovulasyon-2)
    [InlineData(13, CyclePhase.Ovulation)]  // pencerenin başı (ovulasyon-1)
    [InlineData(15, CyclePhase.Ovulation)]  // pencerenin sonu (ovulasyon+1)
    [InlineData(16, CyclePhase.Luteal)]     // ilk luteal gün (ovulasyon+2)
    [InlineData(28, CyclePhase.Luteal)]     // son luteal gün (C)
    public void PhaseBoundaries_AreExact(int cycleDay, CyclePhase expected)
    {
        Assert.Equal(expected, CalculateOnDay(cycleDay).CurrentPhase);
    }

    [Fact]
    public void OvulationWindow_TakesPriorityOverFollicular()
    {
        // Orijinal spec'te foliküler "P+1..ovulasyon-1" olarak tanımlıydı ve
        // ovulasyon penceresiyle 13. günde çakışıyordu. Ovulasyon önceliklidir.
        Assert.Equal(CyclePhase.Ovulation, CalculateOnDay(13).CurrentPhase);
    }

    // --- Düzensiz döngü uyarısı ------------------------------------------

    [Theory]
    [InlineData(19, true)]  // 21'in altı
    [InlineData(21, false)] // alt sınır dahil
    [InlineData(28, false)]
    [InlineData(35, false)] // üst sınır dahil
    [InlineData(36, true)]  // 35'in üstü
    public void IrregularCycle_IsFlagged(int cycleLength, bool expectedIrregular)
    {
        var result = PhaseCalculator.Calculate(
            PeriodStart, cycleLength, PeriodLength, PeriodStart);

        Assert.Equal(expectedIrregular, result.IsIrregular);
    }

    // --- Geciken adet -----------------------------------------------------

    [Fact]
    public void LatePeriod_StaysLutealAndIsFlagged()
    {
        // 31. gün: 28 günlük döngüde adet 3 gün gecikmiş.
        var result = CalculateOnDay(31);

        Assert.Equal(CyclePhase.Luteal, result.CurrentPhase);
        Assert.True(result.IsPeriodLate);
    }

    // --- Kısa döngüde foliküler fazın hiç oluşmaması ----------------------

    [Fact]
    public void VeryShortCycle_SkipsFollicularWithoutMisclassifying()
    {
        // C=21 → ovulasyon günü 7, pencere 6..8. P=5 olduğundan foliküler
        // için yer kalmaz (6. gün doğrudan ovulasyon penceresine düşer).
        var result = PhaseCalculator.Calculate(
            PeriodStart, 21, 5, PeriodStart.AddDays(5));

        Assert.Equal(CyclePhase.Ovulation, result.CurrentPhase);
    }

    // --- Geçersiz girdiler ------------------------------------------------

    [Fact]
    public void TodayBeforePeriodStart_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PhaseCalculator.Calculate(
                PeriodStart, CycleLength, PeriodLength,
                PeriodStart.AddDays(-1)));
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(28, 0)]
    public void NonPositiveLengths_Throw(int cycleLength, int periodLength)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PhaseCalculator.Calculate(
                PeriodStart, cycleLength, periodLength, PeriodStart));
    }
}
