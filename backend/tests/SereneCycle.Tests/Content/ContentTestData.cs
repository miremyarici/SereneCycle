using SereneCycle.Domain.Content;
using SereneCycle.Domain.Cycles;
using SereneCycle.Domain.Entities;

namespace SereneCycle.Tests.Content;

/// <summary>Öneri motoru testlerinin paylaştığı küçük yardımcılar.</summary>
internal static class ContentTestData
{
    /// <summary>
    /// Tek bir aday. Faz ve tür seçimin dışında kaldığı için (adaylar
    /// motora zaten filtrelenmiş gelir) sabit tutulur.
    /// </summary>
    public static ContentItem Item(
        int id,
        IEnumerable<TasteTag>? tastes = null,
        IEnumerable<ContraTag>? contras = null,
        int? durationMinutes = null) =>
        new()
        {
            Id = id,
            Phase = CyclePhase.Menstrual,
            Type = ContentType.FoodDo,
            Title = $"Öğe {id}",
            Body = "Test gerekçesi.",
            TagMask = TasteTags.Of(tastes ?? []),
            ContraMask = ContraTags.Of(contras ?? []),
            DurationMinutes = durationMinutes
        };

    /// <summary>Hiçbir etiketin öne çıkmadığı θ vektörü.</summary>
    public static double[] NeutralScores()
    {
        var scores = new double[TasteTags.Count];
        Array.Fill(scores, 0.5);

        return scores;
    }

    /// <summary>Belirli etiketleri öne çıkaran θ vektörü.</summary>
    public static double[] ScoresFavouring(params TasteTag[] favourites)
    {
        var scores = NeutralScores();

        foreach (var tag in favourites)
        {
            scores[(int)tag] = 1;
        }

        return scores;
    }

    public static IReadOnlyList<int> IdsOf(IEnumerable<ContentItem> items) =>
        [.. items.Select(item => item.Id)];
}
