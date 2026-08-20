namespace SereneCycle.Domain.Content;

/// <summary>
/// Beta(α, β) örnekleyici. .NET'te hazır Beta dağılımı yok, gerekmiyor da:
/// <c>X ~ Gamma(α,1)</c> ve <c>Y ~ Gamma(β,1)</c> iken
/// <c>X / (X + Y) ~ Beta(α, β)</c>.
///
/// Gamma için Marsaglia-Tsang (2000) kabul-ret yöntemi kullanılır; beklenen
/// maliyet O(1), α ≥ 1 için kabul oranı %95'in üzerinde.
///
/// <see cref="Random"/> dışarıdan verilir: <c>PhaseCalculator</c>'daki
/// "bugünü dışarıdan al" disiplininin aynısı — testler tohumla
/// deterministik olsun diye.
/// </summary>
public static class BetaSampler
{
    /// <summary>Marsaglia-Tsang'ın kabul eşiğindeki sabiti.</summary>
    private const double SquaredTailConstant = 0.0331;

    public static double Sample(Random random, double alpha, double beta)
    {
        ArgumentNullException.ThrowIfNull(random);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(alpha);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(beta);

        var x = SampleGamma(random, alpha);
        var y = SampleGamma(random, beta);
        var total = x + y;

        // İki örnek de sıfıra yuvarlandıysa oran tanımsız; bu durumda
        // "bilgi yok" anlamına gelen 0.5 en zararsız cevap.
        return total > 0 ? x / total : 0.5;
    }

    /// <summary>Ölçek 1 olan Gamma(<paramref name="shape"/>) örneği.</summary>
    private static double SampleGamma(Random random, double shape)
    {
        // α &lt; 1'de yöntem doğrudan çalışmaz; Gamma(α) = Gamma(α+1)·U^(1/α)
        // özdeşliğiyle şekil parametresi 1'in üstüne taşınır.
        if (shape < 1)
        {
            var boosted = SampleGamma(random, shape + 1);
            return boosted * Math.Pow(random.NextDouble(), 1 / shape);
        }

        var d = shape - (1.0 / 3.0);
        var c = 1 / Math.Sqrt(9 * d);

        while (true)
        {
            double normal;
            double v;

            do
            {
                normal = NextStandardNormal(random);
                v = 1 + (c * normal);
            }
            while (v <= 0);

            v = v * v * v;

            var uniform = random.NextDouble();
            var squared = normal * normal;

            // Ucuz sınama; çağrıların büyük çoğunluğu burada biter.
            if (uniform < 1 - (SquaredTailConstant * squared * squared))
            {
                return d * v;
            }

            if (Math.Log(uniform)
                < (0.5 * squared) + (d * (1 - v + Math.Log(v))))
            {
                return d * v;
            }
        }
    }

    /// <summary>
    /// Box-Muller dönüşümü. Üretilen iki normalden biri atılır: saklamak
    /// örnekleyiciyi durumlu yapar ve aynı tohumun aynı diziyi vermesini
    /// çağrı sırasına bağlar.
    /// </summary>
    private static double NextStandardNormal(Random random)
    {
        // 1 - u: NextDouble() 0 döndürebilir, log(0) tanımsız.
        var radius = Math.Sqrt(-2 * Math.Log(1 - random.NextDouble()));
        var angle = 2 * Math.PI * random.NextDouble();

        return radius * Math.Cos(angle);
    }
}
