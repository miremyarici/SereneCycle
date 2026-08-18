using SereneCycle.Application.Phases;
using SereneCycle.Domain.Risk;

namespace SereneCycle.Application.Risk;

/// <summary>
/// Risk işaretlerinin kullanıcıya görünen metinleri. Domain katmanı yalnızca
/// kod ve sayı üretir; dil burada durur.
///
/// Ton bilinçli: hiçbir cümle teşhis kurmuyor, "olabilir" ve "takip etmeye
/// değer" ile sınırlı kalıyor. Kullanıcıyı korkutmak değil, kendi verisine
/// bakmaya davet etmek amaç.
/// </summary>
public static class RiskContent
{
    public static string TitleOf(RiskFlagCode code) => code switch
    {
        RiskFlagCode.ProlongedBleeding => "Uzun süren kanama",
        RiskFlagCode.HeavyBleeding => "Üst üste yoğun günler",
        RiskFlagCode.IntermenstrualBleeding => "Adet arası kanama",
        RiskFlagCode.UnusualBloodColor => "Alışılmadık renk",
        RiskFlagCode.VeryShortPeriod => "Çok kısa adet",
        RiskFlagCode.MidCycleSpotting => "Adet dışı lekelenme",
        RiskFlagCode.CycleLengthDeviation => "Kendi normalinden sapma",
        RiskFlagCode.ProlongedAbsence => "Uzun süredir adet yok",
        RiskFlagCode.FrequentPain => "Sık ağrı kaydı",
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, null)
    };

    public static string DetailOf(RiskFlag flag)
    {
        ArgumentNullException.ThrowIfNull(flag);

        return flag.Code switch
        {
            RiskFlagCode.ProlongedBleeding =>
                $"Bu döngüde {flag.Value} gün üst üste kanama kaydettin. "
                + "Bir haftayı aşan kanamalar takip etmeye değer.",

            RiskFlagCode.HeavyBleeding =>
                $"{flag.Value} gün üst üste \"çok\" şiddetinde kanama "
                + "kaydettin.",

            RiskFlagCode.IntermenstrualBleeding =>
                "Kanama bittikten sonra bu döngü içinde yeniden kanama "
                + "kaydettin"
                + (flag.Value > 1 ? $" ({flag.Value} kez)." : "."),

            RiskFlagCode.UnusualBloodColor =>
                "Bu döngüde gri veya turuncu tonda akıntı kaydettin. Bu "
                + "tonlar bazen bir enfeksiyona işaret edebilir.",

            RiskFlagCode.VeryShortPeriod =>
                $"Tamamlanan bu döngüde yalnızca {flag.Value} gün kanama "
                + "kaydedildi. Kaydetmeyi atladıysan bu satırı yok sayabilirsin.",

            RiskFlagCode.MidCycleSpotting =>
                $"Adetinin dışında {flag.Value} gün lekelenme kaydettin. "
                + "Ovulasyon çevresindeki hafif lekelenme sık görülür.",

            RiskFlagCode.CycleLengthDeviation =>
                $"Son döngün, kendi {flag.ReferenceValue} günlük ortalamana "
                + $"göre {Math.Abs(flag.Value)} gün "
                + (flag.Value > 0 ? "uzun" : "kısa")
                + " sürdü.",

            RiskFlagCode.ProlongedAbsence =>
                $"Son adet başlangıcından {flag.Value} gün geçti ve yeni bir "
                + "adet kaydı yok.",

            RiskFlagCode.FrequentPain =>
                $"Bu döngüde {flag.Value} gün ağrı (kramp, bel veya pelvis) "
                + "kaydettin.",

            _ => throw new ArgumentOutOfRangeException(
                nameof(flag), flag.Code, null)
        };
    }

    public static string CardTitleOf(RiskLevel level) => level switch
    {
        RiskLevel.None => "Dikkat çeken bir şey yok",
        RiskLevel.Info => "Takip etmeye değer",
        RiskLevel.Attention => "Dikkat etmeye değer",
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
    };

    /// <param name="loggedDays">Bu döngüde kayıt tutulan gün sayısı.</param>
    public static string CardMessageOf(RiskLevel level, int loggedDays) =>
        level switch
        {
            RiskLevel.None when loggedDays == 0 =>
                "Bu döngüde henüz kayıt yok. Günlerini kaydettikçe döngü "
                + "örüntün burada özetlenir.",

            RiskLevel.None =>
                $"Bu döngüde {loggedDays} gün kaydettin ve kayıtlarında "
                + "dikkat çeken bir örüntü görünmüyor.",

            RiskLevel.Info =>
                "Aşağıdakiler acil bir durum değil, ama önümüzdeki "
                + "döngülerde takip etmek işine yarayabilir.",

            RiskLevel.Attention =>
                "Aşağıdakileri bir sağlık profesyoneliyle konuşmayı "
                + "düşünebilirsin. Bu bir teşhis değil.",

            _ => throw new ArgumentOutOfRangeException(
                nameof(level), level, null)
        };

    /// <summary>Kartın altındaki sabit not; faz içeriğiyle aynı metin.</summary>
    public const string Disclaimer = PhaseContent.MedicalDisclaimer;
}
