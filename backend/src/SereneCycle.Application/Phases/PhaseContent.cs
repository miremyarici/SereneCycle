using SereneCycle.Domain.Cycles;

namespace SereneCycle.Application.Phases;

/// <summary>
/// Faz başlıkları, kısa açıklamalar ve "bu fazda yaygın duygular" metinleri.
///
/// v1'de statik: bunlar tipik eğilimler, kişisel istatistik değil. Dil
/// bilinçli olarak kesinlik iddia etmez ("olabilir", "bazı kişilerde") —
/// döngü fazlarının ruh haline etkisinin kanıt tabanı zayıf-orta.
/// v2'de kullanıcının kendi geçmiş kayıtlarından hesaplanacak.
/// </summary>
public static class PhaseContent
{
    public static string NameOf(CyclePhase phase) => phase switch
    {
        CyclePhase.Menstrual => "Menstrüel Faz",
        CyclePhase.Follicular => "Foliküler Faz",
        CyclePhase.Ovulation => "Ovulasyon",
        CyclePhase.Luteal => "Luteal Faz",
        _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, null)
    };

    public static string DescriptionOf(CyclePhase phase) => phase switch
    {
        CyclePhase.Menstrual =>
            "Hormon seviyeleri en düşük noktasında. Enerjin düşük olabilir; "
            + "dinlenmeye alan tanımak iyi gelebilir.",
        CyclePhase.Follicular =>
            "Östrojen yükseliyor. Enerjinde ve yaratıcılığında bir "
            + "canlanma hissedebilirsin.",
        CyclePhase.Ovulation =>
            "Östrojen zirvede. Birçok kişi bu günlerde kendini en enerjik "
            + "ve sosyal hisseder.",
        CyclePhase.Luteal =>
            "Progesteron yükseliyor. Enerji yavaşça düşebilir; bazı kişilerde "
            + "adet öncesi belirtiler bu dönemde görülür.",
        _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, null)
    };

    public static IReadOnlyList<string> CommonMoodsOf(CyclePhase phase) =>
        phase switch
        {
            CyclePhase.Menstrual => ["Yorgun", "İçe dönük", "Sakin"],
            CyclePhase.Follicular => ["Enerjik", "Odaklı", "Sakin"],
            CyclePhase.Ovulation => ["Enerjik", "Sosyal", "Kendine güvenli"],
            CyclePhase.Luteal => ["Hassas", "Huzursuz", "Yorgun"],
            _ => throw new ArgumentOutOfRangeException(
                nameof(phase), phase, null)
        };

    /// <summary>
    /// Bilgilendirme ekranlarının altında gösterilecek sabit not.
    /// </summary>
    public const string MedicalDisclaimer =
        "Bu içerik bilgilendirme amaçlıdır, tıbbi tavsiye değildir.";
}
