using SereneCycle.Domain.Content;

namespace SereneCycle.Domain.Entities;

/// <summary>
/// Kullanıcının zevk vektörü — kullanıcı başına tek satır, tıpkı
/// <see cref="CycleRiskSummary"/> gibi. Her etiket için bir Beta(α, β)
/// posterior'u tutulur; öneri anında bu posterior'lardan tek bir θ çekilir
/// (Thompson örneklemesi).
///
/// Öğrenilen yüzey bilinçli olarak bundan ibaret: kuralla kesin bilinen
/// her şey (alerji, sakatlık, ekipman, süre, faz) sert filtreye gider,
/// yalnızca "bu kullanıcı bu <i>türden</i> şeyleri seviyor mu" öğrenilir.
///
/// Sayaçlar <see cref="short"/>: kullanıcı başına 2 × 64 × 2 bayt = 256
/// bayt, join yok, tek birincil anahtar getirisi.
/// </summary>
public class UserTasteProfile
{
    /// <summary>Birincil anahtar aynı zamanda kullanıcıya yabancı anahtar.</summary>
    public Guid UserId { get; set; }

    /// <summary>Etiket başına "beğenildi" sayacı; indeks = <see cref="TasteTag"/>.</summary>
    public short[] Alpha { get; set; } = UniformCounts();

    /// <summary>Etiket başına "beğenilmedi" sayacı.</summary>
    public short[] Beta { get; set; } = UniformCounts();

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Hiç geri bildirim görmemiş profil: bütün etiketler Beta(1,1), yani
    /// düzgün dağılım. Satırı olmayan kullanıcı için de bu kullanılır —
    /// okuma yolunda yazma yapmamak için kaydedilmez.
    /// </summary>
    public static UserTasteProfile CreateFor(Guid userId) =>
        new() { UserId = userId };

    /// <summary>
    /// Onboarding anketini prior'a çevirir: <c>α = 1 + s·C</c>,
    /// <c>β = 1 + (1 − s)·C</c>. Ankette geçmeyen etiket Beta(1,1) kalır —
    /// cevaplanmamış soru "nötr" değil, "bilgi yok" demektir.
    ///
    /// Anket yeniden gönderilirse vektör baştan kurulur: anket soğuk
    /// başlangıç aracıdır, öğrenilmişin üzerine eklenen bir sinyal değil.
    /// </summary>
    public void ApplySurvey(
        IEnumerable<TasteTag> liked,
        IEnumerable<TasteTag> disliked)
    {
        ArgumentNullException.ThrowIfNull(liked);
        ArgumentNullException.ThrowIfNull(disliked);

        Alpha = UniformCounts();
        Beta = UniformCounts();

        SetPrior(liked, score: 1);
        SetPrior(disliked, score: 0);

        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Tek bir geri bildirimi öğenin bütün etiketlerine dağıtır. Maliyet
    /// <c>O(popcount(tagMask))</c> ≈ 3 etiket.
    ///
    /// Unutma yalnızca dokunulan etiketlere uygulanır — hiç geri bildirim
    /// almamış bir etiketin bilgisini başka etiketin gözlemi eskitmemeli —
    /// ama o etiketlerde <b>iki sayaca birden</b> uygulanır, yoksa eski
    /// beğeniler yeni olumsuz sinyallerin altında sonsuza kadar kalırdı.
    /// </summary>
    public void ApplyFeedback(long tagMask, ContentFeedback feedback)
    {
        var weight = TasteLearning.WeightOf(feedback);
        var reinforced = feedback == ContentFeedback.Disliked ? Beta : Alpha;

        foreach (var index in TasteTags.IndicesOf(tagMask))
        {
            Alpha[index] = TasteLearning.Forget(Alpha[index]);
            Beta[index] = TasteLearning.Forget(Beta[index]);

            reinforced[index] = TasteLearning.Add(reinforced[index], weight);
        }

        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Etiket başına bir θ örneği. <b>İstek başına bir kez</b> çağrılır,
    /// öğe başına değil: Thompson örneklemesi tek bir dünya hipotezi çekip
    /// ona göre açgözlü davranmaktır; öğe başına yeniden örneklemek
    /// etiketler arası korelasyonu bozar.
    /// </summary>
    public double[] SampleTasteScores(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);

        var scores = new double[TasteTags.Count];

        for (var index = 0; index < scores.Length; index++)
        {
            scores[index] =
                BetaSampler.Sample(random, Alpha[index], Beta[index]);
        }

        return scores;
    }

    private void SetPrior(IEnumerable<TasteTag> tags, double score)
    {
        var (alpha, beta) = TasteLearning.PriorOf(score);

        foreach (var tag in tags)
        {
            Alpha[(int)tag] = alpha;
            Beta[(int)tag] = beta;
        }
    }

    private static short[] UniformCounts()
    {
        var counts = new short[TasteTags.Capacity];
        Array.Fill(counts, TasteLearning.UniformCount);

        return counts;
    }
}
