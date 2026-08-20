using System.Collections.Frozen;
using SereneCycle.Domain.Content;
using SereneCycle.Domain.Entities;
using static SereneCycle.Tests.Content.ContentTestData;

namespace SereneCycle.Tests.Content;

public class RecommenderTests
{
    /// <summary>Kısıt bayrağı ve süresi olmayan geniş katalog.</summary>
    private static readonly IReadOnlyList<ContentItem> WideCatalog =
    [
        Item(1, tastes: [TasteTag.LeafyGreens]),
        Item(2, tastes: [TasteTag.Legumes]),
        Item(3, tastes: [TasteTag.WholeGrains]),
        Item(4, tastes: [TasteTag.Fruit]),
        Item(5, tastes: [TasteTag.Vegetables]),
        Item(6, tastes: [TasteTag.Fermented]),
        Item(7, tastes: [TasteTag.LightSoup]),
        Item(8, tastes: [TasteTag.Spicy])
    ];

    /// <summary>
    /// Yasaklı öğe listede "büyük olasılıkla" çıkmaz değil, <b>asla</b>
    /// çıkmaz. Bu yüzden test tek bir θ örneğine bakmaz: motor binlerce
    /// farklı dünya hipotezinde çalıştırılır, hiçbirinde çıkmamalıdır.
    /// </summary>
    [Fact]
    public void ForbiddenItemNeverAppears_UnderAnyTasteVector()
    {
        var catalog = new List<ContentItem>
        {
            Item(1, tastes: [TasteTag.Fish], contras: [ContraTag.Vegan]),
            Item(2, tastes: [TasteTag.Eggs], contras: [ContraTag.Vegan]),
            Item(3, tastes: [TasteTag.RedMeat],
                contras: [ContraTag.Vegetarian, ContraTag.Vegan]),
            Item(4, tastes: [TasteTag.Legumes]),
            Item(5, tastes: [TasteTag.Vegetables]),
            Item(6, tastes: [TasteTag.Fruit])
        };

        var context = new RecommendationContext
        {
            AvoidMask = ContraTags.Of([ContraTag.Vegan]),
            Count = 3
        };

        var profile = UserTasteProfile.CreateFor(Guid.NewGuid());

        for (var seed = 0; seed < 1000; seed++)
        {
            var scores = profile.SampleTasteScores(new Random(seed));
            var selected = Recommender.Select(catalog, context, scores);

            Assert.DoesNotContain(selected, item => item.Id is 1 or 2 or 3);
        }
    }

    /// <summary>Ekipman da alerjiyle aynı eksende: sert filtre.</summary>
    [Fact]
    public void WithoutEquipment_ItemsRequiringItAreFilteredOut()
    {
        var catalog = new List<ContentItem>
        {
            Item(1, tastes: [TasteTag.Strength],
                contras: [ContraTag.EquipmentDumbbell]),
            Item(2, tastes: [TasteTag.Yoga], contras: [ContraTag.EquipmentMat]),
            Item(3, tastes: [TasteTag.Walking])
        };

        var selected = Recommender.Select(
            catalog,
            new RecommendationContext
            {
                AvoidMask = ContraTags.Of(
                    [ContraTag.EquipmentDumbbell, ContraTag.EquipmentMat]),
                Count = 3
            },
            NeutralScores());

        Assert.Equal([3], IdsOf(selected));
    }

    /// <summary>
    /// Süre bilinen bir kısıttır, öğrenilecek bir tercih değil: 15 dakikası
    /// olan kullanıcıya 45 dakikalık egzersiz hiç gösterilmez.
    /// </summary>
    [Fact]
    public void AvailableMinutes_ExcludesLongerItems()
    {
        var catalog = new List<ContentItem>
        {
            Item(1, tastes: [TasteTag.Walking], durationMinutes: 15),
            Item(2, tastes: [TasteTag.Pilates], durationMinutes: 30),
            Item(3, tastes: [TasteTag.Strength], durationMinutes: 45)
        };

        var selected = Recommender.Select(
            catalog,
            new RecommendationContext { AvailableMinutes = 15, Count = 3 },
            NeutralScores());

        Assert.Equal([1], IdsOf(selected));
    }

    /// <summary>Süresi bilinmeyen öğe (yiyecekler) süre filtresine takılmaz.</summary>
    [Fact]
    public void ItemsWithoutDuration_SurviveTheTimeFilter()
    {
        var selected = Recommender.Select(
            WideCatalog,
            new RecommendationContext { AvailableMinutes = 5, Count = 3 },
            NeutralScores());

        Assert.Equal(3, selected.Count);
    }

    /// <summary>Katalog yeterince genişse son gösterilenler tekrar edilmez.</summary>
    [Fact]
    public void RecentlyShownItems_AreNotRepeated()
    {
        var scores = ScoresFavouring(TasteTag.LeafyGreens, TasteTag.Legumes);
        var context = new RecommendationContext { Count = 3 };

        var yesterday = Recommender.Select(WideCatalog, context, scores);

        var today = Recommender.Select(
            WideCatalog,
            context with { RecentlyShownIds = IdsOf(yesterday).ToFrozenSet() },
            scores);

        Assert.Equal(3, today.Count);
        Assert.Empty(IdsOf(today).Intersect(IdsOf(yesterday)));
    }

    /// <summary>
    /// Katalog dar olduğunda tekrar etmemek, listeyi eksik bırakmaktan daha
    /// önemli değil: kalan yerler dünkü öğelerle tamamlanır.
    /// </summary>
    [Fact]
    public void NarrowCatalog_FallsBackToRecentlyShownItems()
    {
        var catalog = new List<ContentItem>
        {
            Item(1, tastes: [TasteTag.Legumes]),
            Item(2, tastes: [TasteTag.Fruit])
        };

        var selected = Recommender.Select(
            catalog,
            new RecommendationContext
            {
                RecentlyShownIds = new[] { 1, 2 }.ToFrozenSet(),
                Count = 3
            },
            NeutralScores());

        Assert.Equal([1, 2], IdsOf(selected).Order());
    }

    /// <summary>
    /// Sert filtre son gösterilenlerden güçlüdür: yasaklı öğe, liste eksik
    /// kalacak olsa bile geri gelmez.
    /// </summary>
    [Fact]
    public void FallbackNeverBringsBackAForbiddenItem()
    {
        var catalog = new List<ContentItem>
        {
            Item(1, tastes: [TasteTag.Legumes]),
            Item(2, tastes: [TasteTag.Fish], contras: [ContraTag.Vegan])
        };

        var selected = Recommender.Select(
            catalog,
            new RecommendationContext
            {
                AvoidMask = ContraTags.Of([ContraTag.Vegan]),
                RecentlyShownIds = new[] { 1 }.ToFrozenSet(),
                Count = 3
            },
            NeutralScores());

        Assert.Equal([1], IdsOf(selected));
    }

    /// <summary>Skor, öğenin etiketlerinin θ ortalamasıdır.</summary>
    [Fact]
    public void HigherScoringTags_RankFirst()
    {
        var catalog = new List<ContentItem>
        {
            Item(1, tastes: [TasteTag.Legumes]),
            Item(2, tastes: [TasteTag.Yoga]),
            Item(3, tastes: [TasteTag.Fruit])
        };

        var selected = Recommender.Select(
            catalog,
            new RecommendationContext { Count = 1 },
            ScoresFavouring(TasteTag.Yoga));

        Assert.Equal([2], IdsOf(selected));
    }

    /// <summary>Etiketsiz öğe ne öne çıkar ne bastırılır.</summary>
    [Fact]
    public void UntaggedItem_ScoresNeutral()
    {
        var catalog = new List<ContentItem>
        {
            Item(1),
            Item(2, tastes: [TasteTag.Yoga]),
            Item(3, tastes: [TasteTag.Fruit])
        };

        var scores = NeutralScores();
        scores[(int)TasteTag.Yoga] = 1;
        scores[(int)TasteTag.Fruit] = 0;

        var selected = Recommender.Select(
            catalog, new RecommendationContext { Count = 2 }, scores);

        Assert.Equal([2, 1], IdsOf(selected));
    }

    /// <summary>Eşit skorda sıra Id'ye göre sabitlenir; liste zıplamaz.</summary>
    [Fact]
    public void EqualScores_AreBrokenByIdDeterministically()
    {
        var catalog = new List<ContentItem>
        {
            Item(5, tastes: [TasteTag.Legumes]),
            Item(2, tastes: [TasteTag.Legumes]),
            Item(9, tastes: [TasteTag.Legumes])
        };

        var selected = Recommender.Select(
            catalog, new RecommendationContext { Count = 2 }, NeutralScores());

        Assert.Equal([2, 5], IdsOf(selected));
    }

    [Fact]
    public void EmptyCatalog_ReturnsEmptyList()
    {
        var selected = Recommender.Select(
            [], new RecommendationContext { Count = 3 }, NeutralScores());

        Assert.Empty(selected);
    }

    [Fact]
    public void NonPositiveCount_ReturnsEmptyList()
    {
        var selected = Recommender.Select(
            WideCatalog,
            new RecommendationContext { Count = 0 },
            NeutralScores());

        Assert.Empty(selected);
    }
}
