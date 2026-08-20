using SereneCycle.Domain.Content;
using SereneCycle.Domain.Cycles;
using SereneCycle.Domain.Entities;
using static SereneCycle.Tests.Content.ContentTestData;

namespace SereneCycle.Tests.Content;

/// <summary>
/// Günlük tohumun tek işi: listeyi gün içinde sabitlemek, gün değişince
/// tazelemek. Önbellek yerine bu kullanıldığı için garantinin testle
/// korunması şart.
/// </summary>
public class DailySeedTests
{
    private static readonly Guid User =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly Guid OtherUser =
        Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static readonly DateOnly Today = new(2026, 8, 18);

    private static readonly IReadOnlyList<ContentItem> Catalog =
    [
        Item(1, tastes: [TasteTag.LeafyGreens]),
        Item(2, tastes: [TasteTag.Legumes]),
        Item(3, tastes: [TasteTag.WholeGrains]),
        Item(4, tastes: [TasteTag.Fruit]),
        Item(5, tastes: [TasteTag.Vegetables]),
        Item(6, tastes: [TasteTag.Fermented]),
        Item(7, tastes: [TasteTag.LightSoup]),
        Item(8, tastes: [TasteTag.Spicy]),
        Item(9, tastes: [TasteTag.NutsAndSeeds]),
        Item(10, tastes: [TasteTag.Dairy]),
        Item(11, tastes: [TasteTag.Poultry]),
        Item(12, tastes: [TasteTag.Sweet])
    ];

    [Fact]
    public void SameUserSameDaySamePhase_ProducesTheSameSeed()
    {
        Assert.Equal(
            DailySeed.Of(User, Today, CyclePhase.Luteal),
            DailySeed.Of(User, Today, CyclePhase.Luteal));
    }

    [Fact]
    public void NextDay_ProducesADifferentSeed()
    {
        Assert.NotEqual(
            DailySeed.Of(User, Today, CyclePhase.Luteal),
            DailySeed.Of(User, Today.AddDays(1), CyclePhase.Luteal));
    }

    [Fact]
    public void DifferentUsers_ProduceDifferentSeeds()
    {
        Assert.NotEqual(
            DailySeed.Of(User, Today, CyclePhase.Luteal),
            DailySeed.Of(OtherUser, Today, CyclePhase.Luteal));
    }

    [Fact]
    public void DifferentPhases_ProduceDifferentSeeds()
    {
        Assert.NotEqual(
            DailySeed.Of(User, Today, CyclePhase.Luteal),
            DailySeed.Of(User, Today, CyclePhase.Menstrual));
    }

    /// <summary>
    /// Kullanıcı ekranı yenilediğinde liste zıplamamalı: aynı gün içindeki
    /// iki çağrı aynı öğeleri aynı sırada verir.
    /// </summary>
    [Fact]
    public void TwoCallsOnTheSameDay_ReturnTheSameList()
    {
        Assert.Equal(
            IdsOf(SelectFor(Today)),
            IdsOf(SelectFor(Today)));
    }

    /// <summary>
    /// Tazelik hissi çeşitlilikten gelir: bir hafta boyunca liste tek bir
    /// öğe üçlüsüne kilitlenmemeli.
    /// </summary>
    [Fact]
    public void OverAWeek_TheListDoesNotStayLocked()
    {
        var lists = Enumerable
            .Range(0, 7)
            .Select(offset => string.Join(
                ",", IdsOf(SelectFor(Today.AddDays(offset)))))
            .Distinct()
            .ToList();

        Assert.True(
            lists.Count > 1,
            $"Yedi günde yalnızca {lists.Count} farklı liste üretildi.");
    }

    private static IReadOnlyList<ContentItem> SelectFor(DateOnly date)
    {
        var profile = UserTasteProfile.CreateFor(User);

        var scores = profile.SampleTasteScores(
            new Random(DailySeed.Of(User, date, CyclePhase.Luteal)));

        return Recommender.Select(
            Catalog, new RecommendationContext { Count = 3 }, scores);
    }
}
