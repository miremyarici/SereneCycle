using SereneCycle.Domain.Cycles;

namespace SereneCycle.Domain.Entities;

/// <summary>
/// Faza göre beslenme/hareket önerisi. Dil bilinçli olarak yumuşaktır:
/// "yasak" değil "öncelik ver / sınırlı tüket" — cycle syncing'in kanıt
/// tabanı zayıf-orta olduğu için her maddede gerekçe verilir.
/// </summary>
public class ContentItem
{
    public int Id { get; set; }

    public CyclePhase Phase { get; set; }

    public ContentType Type { get; set; }

    public required string Title { get; set; }

    /// <summary>Kısa gerekçe — neden bu öneri verildiği.</summary>
    public required string Body { get; set; }
}

public enum ContentType
{
    FoodDo,
    FoodAvoid,
    ExerciseDo,
    ExerciseAvoid
}
