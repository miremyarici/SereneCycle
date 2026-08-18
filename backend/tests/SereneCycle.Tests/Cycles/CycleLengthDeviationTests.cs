using SereneCycle.Domain.Cycles;

namespace SereneCycle.Tests.Cycles;

public class CycleLengthDeviationTests
{
    [Fact]
    public void FewerThanThreeBaselineCycles_IsNeverNotable()
    {
        // Taban yoksa "kendi normalin" diye bir şey de yok.
        var result = CycleLengthDeviation.Evaluate(40, [28, 28]);

        Assert.False(result.IsNotable);
        Assert.Equal(2, result.BaselineCount);
        Assert.Equal(0, result.DeltaDays);
    }

    [Fact]
    public void RegularUser_SmallChangeIsNotNotable()
    {
        // Ortalama 28, sapma 0 → eşik 4 günlük alt sınırdan gelir.
        var result = CycleLengthDeviation.Evaluate(31, [28, 28, 28, 28]);

        Assert.False(result.IsNotable);
        Assert.Equal(3, result.DeltaDays);
        Assert.Equal(28, result.BaselineAverage);
    }

    [Fact]
    public void RegularUser_FourDayChangeIsNotable()
    {
        var result = CycleLengthDeviation.Evaluate(32, [28, 28, 28, 28]);

        Assert.True(result.IsNotable);
        Assert.Equal(4, result.DeltaDays);
    }

    [Fact]
    public void ShorteningIsAlsoNotable()
    {
        var result = CycleLengthDeviation.Evaluate(23, [28, 28, 28, 28]);

        Assert.True(result.IsNotable);
        Assert.Equal(-5, result.DeltaDays);
    }

    [Fact]
    public void FluctuatingUser_NeedsALargerJump()
    {
        // Kendisi zaten 24-34 arasında geziniyor: 5 günlük sapma onun için
        // olağan. Eşik sigma ile birlikte genişliyor.
        int[] baseline = [24, 34, 26, 32, 28, 30];

        var withinNormal = CycleLengthDeviation.Evaluate(34, baseline);
        var beyondNormal = CycleLengthDeviation.Evaluate(45, baseline);

        Assert.True(withinNormal.BaselineStdDev > 2);
        Assert.False(withinNormal.IsNotable);
        Assert.True(beyondNormal.IsNotable);
    }

    [Fact]
    public void OnlyMostRecentCyclesWithinWindowFormTheBaseline()
    {
        // Penceredeki 6 döngü hep 28; öncesindeki uçuk değerler taban
        // dışında kalmalı, yoksa sigma şişer ve sapma gizlenirdi.
        int[] baseline = [28, 28, 28, 28, 28, 28, 60, 15, 70];

        var result = CycleLengthDeviation.Evaluate(34, baseline);

        Assert.Equal(6, result.BaselineCount);
        Assert.Equal(28, result.BaselineAverage);
        Assert.Equal(0, result.BaselineStdDev, 6);
        Assert.True(result.IsNotable);
    }

    [Fact]
    public void CustomWindowIsRespected()
    {
        int[] baseline = [30, 30, 30, 28, 28, 28];

        var narrow = CycleLengthDeviation.Evaluate(30, baseline, window: 3);

        Assert.Equal(3, narrow.BaselineCount);
        Assert.Equal(30, narrow.BaselineAverage);
        Assert.Equal(0, narrow.DeltaDays);
    }

    [Fact]
    public void BaselineAverageIsRoundedAwayFromZero()
    {
        // (28+29+30+30)/4 = 29.25 → 29
        var result = CycleLengthDeviation.Evaluate(29, [28, 29, 30, 30]);

        Assert.Equal(29, result.BaselineAverage);
        Assert.Equal(0, result.DeltaDays);
    }
}
