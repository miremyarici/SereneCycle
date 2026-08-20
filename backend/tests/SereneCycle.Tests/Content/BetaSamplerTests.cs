using SereneCycle.Domain.Content;

namespace SereneCycle.Tests.Content;

public class BetaSamplerTests
{
    private const int SampleCount = 20_000;

    /// <summary>
    /// Tohumlanabilirlik tasarımın parçası: liste günlük tohumla üretildiği
    /// için aynı tohum aynı örnekleri vermezse "aynı gün aynı liste"
    /// garantisi de çöker.
    /// </summary>
    [Fact]
    public void SameSeed_ProducesTheSameSequence()
    {
        var first = Draw(new Random(42), 3, 7, count: 50);
        var second = Draw(new Random(42), 3, 7, count: 50);

        Assert.Equal(first, second);
    }

    [Fact]
    public void DifferentSeeds_ProduceDifferentSequences()
    {
        var first = Draw(new Random(1), 3, 7, count: 50);
        var second = Draw(new Random(2), 3, 7, count: 50);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void SamplesAlwaysStayInTheUnitInterval()
    {
        var random = new Random(7);

        foreach (var (alpha, beta) in
                 new[] { (0.5, 0.5), (1.0, 1.0), (200.0, 1.0), (1.0, 200.0) })
        {
            for (var i = 0; i < 1000; i++)
            {
                var sample = BetaSampler.Sample(random, alpha, beta);

                Assert.InRange(sample, 0, 1);
            }
        }
    }

    /// <summary>
    /// Beta(1,1) düzgün dağılımdır: ortalaması 0.5'e yakın olmalı ve
    /// örnekler tek bir bölgede yığılmamalı.
    /// </summary>
    [Fact]
    public void BetaOneOne_IsApproximatelyUniform()
    {
        var samples = Draw(new Random(2026), 1, 1, SampleCount);

        Assert.Equal(0.5, samples.Average(), precision: 1);

        var buckets = new int[10];

        foreach (var sample in samples)
        {
            buckets[Math.Min((int)(sample * 10), 9)]++;
        }

        var expected = SampleCount / 10.0;

        // Her kova beklenenin %10'u içinde kalmalı; düzgün dağılımda bu
        // aralık 20 bin örnekte fazlasıyla geniş.
        Assert.All(
            buckets,
            count => Assert.InRange(count, expected * 0.9, expected * 1.1));
    }

    /// <summary>α ≫ β iken örnekler 1'e yaklaşır: "bu etiketi seviyor".</summary>
    [Fact]
    public void AlphaMuchLargerThanBeta_ConcentratesNearOne()
    {
        var samples = Draw(new Random(11), alpha: 200, beta: 1, SampleCount);

        Assert.True(samples.Average() > 0.98);
        Assert.True(samples.Min() > 0.5);
    }

    /// <summary>Simetrik durum: β ≫ α iken örnekler 0'a yaklaşır.</summary>
    [Fact]
    public void BetaMuchLargerThanAlpha_ConcentratesNearZero()
    {
        var samples = Draw(new Random(11), alpha: 1, beta: 200, SampleCount);

        Assert.True(samples.Average() < 0.02);
        Assert.True(samples.Max() < 0.5);
    }

    /// <summary>Ortalama α/(α+β)'ya yakınsar.</summary>
    [Theory]
    [InlineData(2, 8, 0.2)]
    [InlineData(5, 5, 0.5)]
    [InlineData(9, 3, 0.75)]
    public void MeanMatchesTheAnalyticValue(
        double alpha, double beta, double expected)
    {
        var samples = Draw(new Random(3), alpha, beta, SampleCount);

        Assert.Equal(expected, samples.Average(), tolerance: 0.01);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, 1)]
    public void NonPositiveParameters_Throw(double alpha, double beta) =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => BetaSampler.Sample(new Random(1), alpha, beta));

    private static double[] Draw(
        Random random, double alpha, double beta, int count) =>
        [.. Enumerable
            .Range(0, count)
            .Select(_ => BetaSampler.Sample(random, alpha, beta))];
}
