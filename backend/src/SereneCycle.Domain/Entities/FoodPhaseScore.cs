using SereneCycle.Domain.Cycles;

namespace SereneCycle.Domain.Entities;

/// <summary>
/// YOLO sınıfı → faz uygunluk eşlemesi. ML servisi yalnızca görüntüyü işler;
/// "bu fazda iyi mi" kararı bu tablodan burada verilir.
/// </summary>
public class FoodPhaseScore
{
    /// <summary>YOLO model sınıf adı, örn. "apple".</summary>
    public required string FoodClass { get; set; }

    public CyclePhase Phase { get; set; }

    public FoodSuitability Score { get; set; }

    /// <summary>Kullanıcıya gösterilecek kısa gerekçe.</summary>
    public required string Reason { get; set; }
}

/// <summary>
/// Bilinçli olarak 3 kademe — "10 üzerinden 7.4" gibi sahte hassasiyet yok.
/// </summary>
public enum FoodSuitability
{
    Limited = -1,
    Neutral = 0,
    Good = 1
}
