namespace SereneCycle.Domain.Risk;

/// <summary>
/// Bir işaretin ağırlığı. Kart bütününün seviyesi, işaretlerin en
/// yükseğidir. Bilinçli olarak üç kademe: "acil" demiyoruz, çünkü bu
/// uygulama teşhis koymuyor.
/// </summary>
public enum RiskLevel
{
    /// <summary>Dikkat çeken bir şey yok.</summary>
    None = 0,

    /// <summary>Bilgi amaçlı; örüntüyü takip etmeye değer.</summary>
    Info = 1,

    /// <summary>Bir sağlık profesyoneline danışmayı düşündürecek kadar belirgin.</summary>
    Attention = 2
}

/// <summary>
/// Risk motorunun üretebildiği işaretlerin kapalı listesi. Metinler
/// <c>RiskContent</c> içinde; burada yalnızca kod var ki domain katmanı
/// sunum diline bağlanmasın.
/// </summary>
public enum RiskFlagCode
{
    /// <summary>7 günden uzun süren kanama (menoraji sinyali).</summary>
    ProlongedBleeding,

    /// <summary>Üst üste birkaç gün "çok" şiddetinde kanama.</summary>
    HeavyBleeding,

    /// <summary>Adet bittikten sonra aynı döngü içinde yeniden kanama.</summary>
    IntermenstrualBleeding,

    /// <summary>Gri/turuncu gibi alışılmadık akıntı rengi.</summary>
    UnusualBloodColor,

    /// <summary>Tamamlanmış döngüde çok az kanama günü (hipomenore sinyali).</summary>
    VeryShortPeriod,

    /// <summary>Adet penceresi dışında yinelenen lekelenme.</summary>
    MidCycleSpotting,

    /// <summary>Son döngü, kullanıcının kendi ortalamasından belirgin sapmış.</summary>
    CycleLengthDeviation,

    /// <summary>Uzun süredir yeni adet başlamamış (amenore sinyali).</summary>
    ProlongedAbsence,

    /// <summary>Döngü boyunca sık ağrı kaydı.</summary>
    FrequentPain
}

/// <summary>
/// Tek bir işaret. <paramref name="Value"/> ve
/// <paramref name="ReferenceValue"/> metni üretmek için gereken sayılardır
/// (kaç gün, kaç gün sapma, hangi ortalamaya göre); anlamı koda göre değişir.
/// </summary>
public sealed record RiskFlag(
    RiskFlagCode Code,
    RiskLevel Level,
    int Value = 0,
    int ReferenceValue = 0);

/// <summary>Risk motorunun çıktısı: seviye + işaretler.</summary>
public sealed record RiskAssessment(
    RiskLevel Level,
    IReadOnlyList<RiskFlag> Flags)
{
    public static readonly RiskAssessment Empty =
        new(RiskLevel.None, []);
}
