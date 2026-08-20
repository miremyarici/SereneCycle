using System.Collections.Frozen;
using SereneCycle.Domain.Entities;

namespace SereneCycle.Domain.Content;

/// <summary>
/// Bir öneri isteğinin öğrenilmeyen tarafı: hepsi kesin bilinen kısıtlar.
/// Bağlam öğrenilmez, filtrelenir.
/// </summary>
public sealed record RecommendationContext
{
    /// <summary>Bir listede gösterilen öğe sayısı.</summary>
    public const int DefaultCount = 3;

    /// <summary>
    /// Kullanıcının kısıt bayrakları. Öğenin <c>ContraMask</c>'i ile
    /// kesişen her öğe aday kümesinden <b>kesin olarak</b> çıkar; bu
    /// garanti istatistiksel değil, birim testle kanıtlanabilir.
    /// </summary>
    public long AvoidMask { get; init; }

    /// <summary>
    /// Kullanıcının o an ayırabildiği süre. Verilmezse süre filtresi
    /// çalışmaz; süresi bilinmeyen öğe (yiyecekler) hiçbir zaman elenmez.
    /// </summary>
    public int? AvailableMinutes { get; init; }

    /// <summary>
    /// Son gösterilenler. Sert kısıt değil: katalog dar olduğunda liste
    /// boş kalmasın diye eksik yer bunlardan tamamlanır.
    /// </summary>
    public IReadOnlySet<int> RecentlyShownIds { get; init; } =
        FrozenSet<int>.Empty;

    public int Count { get; init; } = DefaultCount;

    /// <summary>
    /// Sert filtre — O(1)/öğe. Kısıtlar baştan kesin bilindiği için
    /// "constrained bandit" yerine bu kullanılır: yasaklı öğeyi bandit hiç
    /// görmez.
    /// </summary>
    public bool Allows(ContentItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return (item.ContraMask & AvoidMask) == 0
               && FitsInAvailableTime(item.DurationMinutes);
    }

    private bool FitsInAvailableTime(int? durationMinutes) =>
        AvailableMinutes is not { } available
        || durationMinutes is not { } duration
        || duration <= available;
}

/// <summary>
/// Öneri listesini üreten saf seçim fonksiyonu — <c>PhaseCalculator</c> ve
/// <c>RiskEvaluator</c> gibi hiçbir I/O, zaman veya rastgelelik kaynağı
/// içermez. Rastgelelik θ örneklerinin içinde gelir, böylece "aynı gün aynı
/// liste" garantisi tek bir tohum kararına iner.
///
/// Maliyet: filtre + skor O(N), top-k O(N log k). Hiçbir terimde kullanıcı
/// sayısı veya kullanıcının geçmiş uzunluğu yok.
/// </summary>
public static class Recommender
{
    /// <summary>
    /// Etiketsiz öğenin skoru. Ne öne çıkarılır ne bastırılır: Beta(1,1)
    /// örneklerinin beklenen değeriyle aynı yerde durur.
    /// </summary>
    public const double NeutralScore = 0.5;

    public static IReadOnlyList<ContentItem> Select(
        IReadOnlyList<ContentItem> candidates,
        RecommendationContext context,
        IReadOnlyList<double> tasteScores)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(tasteScores);

        if (context.Count <= 0)
        {
            return [];
        }

        var fresh = new TopItems(context.Count);
        var seenBefore = new TopItems(context.Count);

        foreach (var item in candidates)
        {
            if (!context.Allows(item))
            {
                continue;
            }

            var score = ScoreOf(item.TagMask, tasteScores);
            var bucket =
                context.RecentlyShownIds.Contains(item.Id) ? seenBefore : fresh;

            bucket.Offer(item, score);
        }

        var selected = fresh.ToDescendingList();

        if (selected.Count < context.Count)
        {
            // Katalog bu faz için dar kalmış. Tekrar etmemek, listeyi eksik
            // bırakmaktan daha önemli değil.
            selected.AddRange(
                seenBefore.ToDescendingList().Take(context.Count - selected.Count));
        }

        return selected;
    }

    /// <summary>
    /// Öğenin skoru = etiketlerinin θ ortalaması. Maliyet
    /// <c>O(popcount(tagMask))</c> ≈ 3.
    /// </summary>
    private static double ScoreOf(long tagMask, IReadOnlyList<double> tasteScores)
    {
        var total = 0.0;
        var count = 0;

        foreach (var index in TasteTags.IndicesOf(tagMask))
        {
            // Vektörde karşılığı olmayan etiket (katalog ileride eklenip
            // profil henüz büyümediyse) skoru bozmasın.
            if (index >= tasteScores.Count)
            {
                continue;
            }

            total += tasteScores[index];
            count++;
        }

        return count == 0 ? NeutralScore : total / count;
    }

    /// <summary>
    /// En yüksek skorlu k öğeyi tutan sınırlı min-heap: bütün adayları
    /// sıralamak yerine O(m log k) çalışır, O(k) yer tutar.
    /// </summary>
    private sealed class TopItems(int capacity)
    {
        private readonly PriorityQueue<ContentItem, ItemRank> _heap =
            new(capacity);

        public void Offer(ContentItem item, double score)
        {
            _heap.Enqueue(item, new ItemRank(score, item.Id));

            if (_heap.Count > capacity)
            {
                // Kökte her zaman en zayıf aday durur.
                _heap.Dequeue();
            }
        }

        public List<ContentItem> ToDescendingList()
        {
            var ascending = new List<ContentItem>(_heap.Count);

            while (_heap.TryDequeue(out var item, out _))
            {
                ascending.Add(item);
            }

            ascending.Reverse();

            return ascending;
        }
    }

    /// <summary>
    /// Sıralama anahtarı. Eşit skorda küçük <c>Id</c> kazanır: skorlar
    /// eşitlendiğinde de aynı girdi aynı listeyi versin diye kural sabit.
    /// </summary>
    private readonly record struct ItemRank(double Score, int Id)
        : IComparable<ItemRank>
    {
        public int CompareTo(ItemRank other)
        {
            var byScore = Score.CompareTo(other.Score);

            // Küçük Id "daha güçlü" sayılır, yani min-heap'ten en son atılır.
            return byScore != 0 ? byScore : other.Id.CompareTo(Id);
        }
    }
}
