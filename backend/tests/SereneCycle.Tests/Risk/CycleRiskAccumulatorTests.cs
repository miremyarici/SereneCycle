using SereneCycle.Domain.Entities;
using SereneCycle.Domain.Risk;

namespace SereneCycle.Tests.Risk;

public class CycleRiskAccumulatorTests
{
    /// <summary>Kanama günü kısayolu: verilen gün numarasında kanama.</summary>
    private static RiskDay Bleeding(
        int dayIndex,
        FlowIntensity flow = FlowIntensity.Medium,
        BloodColor? color = null,
        long symptomMask = 0) =>
        new(dayIndex, true, flow, color, false, symptomMask);

    private static RiskDay Spotting(int dayIndex) =>
        new(dayIndex, false, null, null, true, 0);

    private static CycleRiskSignals Summarize(params RiskDay[] days)
    {
        var accumulator = new CycleRiskAccumulator();

        foreach (var day in days)
        {
            accumulator.Add(day);
        }

        return accumulator.Build();
    }

    [Fact]
    public void NoDays_ProducesEmptySignals()
    {
        var signals = Summarize();

        Assert.Equal(CycleRiskSignals.Empty, signals);
        Assert.False(signals.HasAnyLog);
    }

    [Fact]
    public void ConsecutiveBleedingDays_FormOneStreak()
    {
        var signals = Summarize(
            Bleeding(1), Bleeding(2), Bleeding(3), Bleeding(4));

        Assert.Equal(4, signals.BleedingDays);
        Assert.Equal(4, signals.LongestBleedingStreak);
        Assert.Equal(0, signals.BleedingRestartCount);
    }

    [Fact]
    public void GapBreaksStreakAndLongestIsKept()
    {
        // 1-2-3 kanama, 4-5 boş, 6 kanama → en uzun seri 3, seri sayacı sıfırlanır.
        var signals = Summarize(
            Bleeding(1), Bleeding(2), Bleeding(3), Bleeding(6));

        Assert.Equal(4, signals.BleedingDays);
        Assert.Equal(3, signals.LongestBleedingStreak);
    }

    [Fact]
    public void SingleMissingDayIsNotCountedAsRestart()
    {
        // Arada tek bir kanamasız gün var: kullanıcı kaydı atlamış da olabilir,
        // bunu "adet arası kanama" saymıyoruz.
        var signals = Summarize(Bleeding(1), Bleeding(2), Bleeding(4));

        Assert.Equal(0, signals.BleedingRestartCount);
        Assert.Equal(2, signals.LongestBleedingStreak);
    }

    [Fact]
    public void BleedingAfterTwoFreeDaysCountsAsRestart()
    {
        // 1-2 kanama, 3-4 boş, 5 kanama → yeniden başlamış.
        var signals = Summarize(Bleeding(1), Bleeding(2), Bleeding(5));

        Assert.Equal(1, signals.BleedingRestartCount);
    }

    [Fact]
    public void HeavyStreakCountsOnlyHeavyDays()
    {
        // Yoğun günler ardışık değil: 1 ve 3 yoğun, 2 orta → en uzun seri 1.
        var signals = Summarize(
            Bleeding(1, FlowIntensity.Heavy),
            Bleeding(2, FlowIntensity.Medium),
            Bleeding(3, FlowIntensity.Heavy));

        Assert.Equal(2, signals.HeavyDays);
        Assert.Equal(1, signals.LongestHeavyStreak);

        var consecutive = Summarize(
            Bleeding(1, FlowIntensity.Heavy),
            Bleeding(2, FlowIntensity.Heavy),
            Bleeding(3, FlowIntensity.Heavy));

        Assert.Equal(3, consecutive.LongestHeavyStreak);
    }

    [Fact]
    public void BloodColorsAccumulateIntoMask()
    {
        var signals = Summarize(
            Bleeding(1, color: BloodColor.Red),
            Bleeding(2, color: BloodColor.Red),
            Bleeding(3, color: BloodColor.Brown));

        Assert.True(
            BloodColorMasks.Contains(signals.BloodColorMask, BloodColor.Red));
        Assert.True(
            BloodColorMasks.Contains(signals.BloodColorMask, BloodColor.Brown));
        Assert.False(
            BloodColorMasks.Contains(signals.BloodColorMask, BloodColor.Gray));
        Assert.Equal(0, signals.BloodColorMask & BloodColorMasks.Unusual);
    }

    [Fact]
    public void SpottingIsSplitByCycleDay()
    {
        // Eşik 8. gün: 3 ve 7 adet penceresinde, 8 ve 15 dışında sayılır.
        var signals = Summarize(
            Spotting(3), Spotting(7), Spotting(8), Spotting(15));

        Assert.Equal(4, signals.SpottingDays);
        Assert.Equal(2, signals.SpottingOutsidePeriodDays);
    }

    [Fact]
    public void SymptomMasksAreOredAndPainDaysCounted()
    {
        var cramps = SymptomMasks.BitOf(1);   // Karın krampları
        var headache = SymptomMasks.BitOf(2); // Baş ağrısı — ağrılı gün sayılmaz
        var backPain = SymptomMasks.BitOf(7); // Bel ağrısı

        var signals = Summarize(
            Bleeding(1, symptomMask: cramps | headache),
            Bleeding(2, symptomMask: headache),
            Bleeding(3, symptomMask: backPain));

        Assert.True(SymptomMasks.Contains(signals.SymptomMask, 1));
        Assert.True(SymptomMasks.Contains(signals.SymptomMask, 2));
        Assert.True(SymptomMasks.Contains(signals.SymptomMask, 7));
        Assert.False(SymptomMasks.Contains(signals.SymptomMask, 5));

        // Yalnızca kramp/bel/pelvis içeren günler ağrılı gün.
        Assert.Equal(2, signals.PainDays);
    }

    [Fact]
    public void SymptomMaskIgnoresIdsOutsideRange()
    {
        Assert.Equal(0L, SymptomMasks.BitOf(0));
        Assert.Equal(0L, SymptomMasks.BitOf(65));
        Assert.Equal(1L, SymptomMasks.BitOf(1));
        Assert.Equal(SymptomMasks.BitOf(3), SymptomMasks.Of([3, 65, 0]));
    }

    [Fact]
    public void SpottingOnABleedingDayIsCountedOnce()
    {
        // Aynı günde hem kanama hem lekelenme işaretlenebilir.
        var signals = Summarize(
            new RiskDay(1, true, FlowIntensity.Light, BloodColor.Brown, true, 0));

        Assert.Equal(1, signals.LoggedDays);
        Assert.Equal(1, signals.BleedingDays);
        Assert.Equal(1, signals.SpottingDays);
    }
}
