using SereneCycle.Domain.Cycles;
using SereneCycle.Domain.Entities;
using SereneCycle.Domain.Risk;

namespace SereneCycle.Tests.Risk;

public class RiskEvaluatorTests
{
    /// <summary>Açık döngü, 10. gün, sapma verisi yok.</summary>
    private static readonly RiskContext OpenCycle =
        new(IsCycleComplete: false, CycleDay: 10, LastCycleDeviation: null);

    private static readonly RiskContext ClosedCycle =
        new(IsCycleComplete: true, CycleDay: 28, LastCycleDeviation: null);

    private static CycleRiskSignals Signals(
        int loggedDays = 5,
        int bleedingDays = 5,
        int longestBleedingStreak = 5,
        int heavyDays = 0,
        int longestHeavyStreak = 0,
        int spottingDays = 0,
        int spottingOutsidePeriodDays = 0,
        int bleedingRestartCount = 0,
        int painDays = 0,
        int bloodColorMask = 0,
        long symptomMask = 0) =>
        new(loggedDays, bleedingDays, longestBleedingStreak, heavyDays,
            longestHeavyStreak, spottingDays, spottingOutsidePeriodDays,
            bleedingRestartCount, painDays, bloodColorMask, symptomMask);

    private static bool Has(RiskAssessment assessment, RiskFlagCode code) =>
        assessment.Flags.Any(f => f.Code == code);

    [Fact]
    public void TypicalCycle_ProducesNoFlags()
    {
        var assessment = RiskEvaluator.Evaluate(
            Signals(
                loggedDays: 6,
                bleedingDays: 5,
                longestBleedingStreak: 5,
                heavyDays: 2,
                longestHeavyStreak: 2,
                spottingDays: 1,
                bloodColorMask: BloodColorMasks.BitOf(BloodColor.Red),
                painDays: 2),
            OpenCycle);

        Assert.Equal(RiskLevel.None, assessment.Level);
        Assert.Empty(assessment.Flags);
    }

    [Fact]
    public void EmptyCycle_ProducesNoFlags()
    {
        var assessment =
            RiskEvaluator.Evaluate(CycleRiskSignals.Empty, OpenCycle);

        Assert.Equal(RiskLevel.None, assessment.Level);
    }

    [Fact]
    public void EightDayBleedingStreak_IsAttention()
    {
        var assessment = RiskEvaluator.Evaluate(
            Signals(loggedDays: 8, bleedingDays: 8, longestBleedingStreak: 8),
            OpenCycle);

        Assert.Equal(RiskLevel.Attention, assessment.Level);

        var flag = Assert.Single(
            assessment.Flags,
            f => f.Code == RiskFlagCode.ProlongedBleeding);
        Assert.Equal(8, flag.Value);
    }

    [Fact]
    public void SevenDayBleedingStreak_IsBelowThreshold()
    {
        var assessment = RiskEvaluator.Evaluate(
            Signals(loggedDays: 7, bleedingDays: 7, longestBleedingStreak: 7),
            OpenCycle);

        Assert.False(Has(assessment, RiskFlagCode.ProlongedBleeding));
    }

    [Fact]
    public void ThreeConsecutiveHeavyDays_IsAttention()
    {
        var assessment = RiskEvaluator.Evaluate(
            Signals(heavyDays: 3, longestHeavyStreak: 3), OpenCycle);

        Assert.Equal(RiskLevel.Attention, assessment.Level);
        Assert.True(Has(assessment, RiskFlagCode.HeavyBleeding));
    }

    [Fact]
    public void BleedingRestart_IsAttention()
    {
        var assessment = RiskEvaluator.Evaluate(
            Signals(bleedingRestartCount: 1), OpenCycle);

        Assert.True(Has(assessment, RiskFlagCode.IntermenstrualBleeding));
    }

    [Theory]
    [InlineData(BloodColor.Gray, true)]
    [InlineData(BloodColor.Orange, true)]
    [InlineData(BloodColor.Red, false)]
    [InlineData(BloodColor.Brown, false)]
    [InlineData(BloodColor.Pink, false)]
    [InlineData(BloodColor.Black, false)]
    public void UnusualColors_AreFlagged(BloodColor color, bool expected)
    {
        var assessment = RiskEvaluator.Evaluate(
            Signals(bloodColorMask: BloodColorMasks.BitOf(color)), OpenCycle);

        Assert.Equal(
            expected, Has(assessment, RiskFlagCode.UnusualBloodColor));
    }

    [Fact]
    public void VeryShortPeriod_OnlyOnCompletedCycle()
    {
        var signals = Signals(
            loggedDays: 1, bleedingDays: 1, longestBleedingStreak: 1);

        // Açık döngüde adet hâlâ sürüyor olabilir: işaret üretilmez.
        Assert.False(Has(
            RiskEvaluator.Evaluate(signals, OpenCycle),
            RiskFlagCode.VeryShortPeriod));

        var closed = RiskEvaluator.Evaluate(signals, ClosedCycle);

        Assert.True(Has(closed, RiskFlagCode.VeryShortPeriod));
        Assert.Equal(RiskLevel.Info, closed.Level);
    }

    [Fact]
    public void NoBleedingLogged_IsNotReportedAsShortPeriod()
    {
        // Hiç kanama kaydı yoksa bu veri eksikliğidir, bulgu değil.
        var assessment = RiskEvaluator.Evaluate(
            Signals(loggedDays: 2, bleedingDays: 0, longestBleedingStreak: 0),
            ClosedCycle);

        Assert.False(Has(assessment, RiskFlagCode.VeryShortPeriod));
        Assert.Equal(RiskLevel.None, assessment.Level);
    }

    [Fact]
    public void MidCycleSpotting_NeedsThreeDays()
    {
        Assert.False(Has(
            RiskEvaluator.Evaluate(
                Signals(spottingDays: 2, spottingOutsidePeriodDays: 2),
                OpenCycle),
            RiskFlagCode.MidCycleSpotting));

        Assert.True(Has(
            RiskEvaluator.Evaluate(
                Signals(spottingDays: 3, spottingOutsidePeriodDays: 3),
                OpenCycle),
            RiskFlagCode.MidCycleSpotting));
    }

    [Fact]
    public void ProlongedAbsence_OnlyOnOpenCycle()
    {
        var lateOpen = new RiskContext(
            IsCycleComplete: false, CycleDay: 95, LastCycleDeviation: null);

        Assert.True(Has(
            RiskEvaluator.Evaluate(CycleRiskSignals.Empty, lateOpen),
            RiskFlagCode.ProlongedAbsence));

        // Kapanmış döngü 95 gün sürmüş olabilir ama adet gelmiş demektir.
        var lateClosed = lateOpen with { IsCycleComplete = true };

        Assert.False(Has(
            RiskEvaluator.Evaluate(CycleRiskSignals.Empty, lateClosed),
            RiskFlagCode.ProlongedAbsence));
    }

    [Fact]
    public void FrequentPain_NeedsFiveDays()
    {
        Assert.False(Has(
            RiskEvaluator.Evaluate(Signals(painDays: 4), OpenCycle),
            RiskFlagCode.FrequentPain));

        var flagged = RiskEvaluator.Evaluate(Signals(painDays: 5), OpenCycle);

        Assert.True(Has(flagged, RiskFlagCode.FrequentPain));
        Assert.Equal(RiskLevel.Info, flagged.Level);
    }

    [Fact]
    public void NotableDeviation_IsCarriedIntoFlag()
    {
        var context = OpenCycle with
        {
            LastCycleDeviation = new CycleLengthDeviationResult(
                IsNotable: true,
                DeltaDays: 6,
                BaselineAverage: 29,
                BaselineCount: 4,
                BaselineStdDev: 1.2)
        };

        var flag = Assert.Single(
            RiskEvaluator.Evaluate(Signals(), context).Flags,
            f => f.Code == RiskFlagCode.CycleLengthDeviation);

        Assert.Equal(6, flag.Value);
        Assert.Equal(29, flag.ReferenceValue);
    }

    [Fact]
    public void UnremarkableDeviation_ProducesNoFlag()
    {
        var context = OpenCycle with
        {
            LastCycleDeviation = new CycleLengthDeviationResult(
                IsNotable: false,
                DeltaDays: 1,
                BaselineAverage: 29,
                BaselineCount: 4,
                BaselineStdDev: 1.2)
        };

        Assert.False(Has(
            RiskEvaluator.Evaluate(Signals(), context),
            RiskFlagCode.CycleLengthDeviation));
    }

    [Fact]
    public void AttentionFlagsAreListedBeforeInfoFlags()
    {
        var assessment = RiskEvaluator.Evaluate(
            Signals(
                loggedDays: 12,
                bleedingDays: 9,
                longestBleedingStreak: 9,
                painDays: 6,
                spottingDays: 3,
                spottingOutsidePeriodDays: 3),
            OpenCycle);

        Assert.Equal(RiskLevel.Attention, assessment.Level);
        Assert.Equal(3, assessment.Flags.Count);
        Assert.Equal(RiskLevel.Attention, assessment.Flags[0].Level);
        Assert.Equal(RiskFlagCode.ProlongedBleeding, assessment.Flags[0].Code);
        Assert.Equal(RiskLevel.Info, assessment.Flags[^1].Level);
    }
}
