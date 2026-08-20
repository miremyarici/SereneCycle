using SereneCycle.Domain.Content;
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

    /// <summary>
    /// Öğenin zevk etiketleri (<see cref="TasteTag"/> maskesi). Öneri
    /// motorunun skoru bu etiketlerin ortalamasıdır.
    /// </summary>
    public long TagMask { get; set; }

    /// <summary>
    /// Öğeyi eleyecek kullanıcı bayrakları (<see cref="ContraTag"/>
    /// maskesi). Kullanıcının <c>AvoidMask</c>'i ile kesişiyorsa öğe
    /// aday kümesine hiç girmez.
    /// </summary>
    public long ContraMask { get; set; }

    /// <summary>
    /// Yaklaşık süre — yalnızca egzersiz önerilerinde dolu. Süresi
    /// bilinmeyen öğe süre filtresine takılmaz.
    /// </summary>
    public int? DurationMinutes { get; set; }
}

public enum ContentType
{
    FoodDo,
    FoodAvoid,
    ExerciseDo,
    ExerciseAvoid
}
