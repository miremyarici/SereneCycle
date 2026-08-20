namespace SereneCycle.Domain.Content;

/// <summary>Tek bir geri bildirimin taşıdığı sinyal.</summary>
public enum ContentFeedback
{
    /// <summary>👍 — en güçlü olumlu sinyal.</summary>
    Liked,

    /// <summary>👎.</summary>
    Disliked,

    /// <summary>
    /// "Tamamladım" — beğeniden zayıf ama gerçek bir olumlu sinyal:
    /// kullanıcı öneriyi beğendiğini söylemedi, yaptığını söyledi.
    /// </summary>
    Completed
}

/// <summary>
/// Öğrenmenin bütün katsayıları tek yerde — <c>RiskThresholds</c> ile aynı
/// disiplin. Hiçbiri kullanıcı sayısına veya geçmiş uzunluğuna bağlı değil.
/// </summary>
public static class TasteLearning
{
    /// <summary>Bilgi yokken Beta(1,1): düzgün dağılım, en yüksek keşif.</summary>
    public const short UniformCount = 1;

    /// <summary>
    /// Onboarding anketinin sahte-sayımı: "bu anket kaç gerçek gözleme
    /// bedel". 4 seçildi — kullanıcının 4 gerçek geri bildiriminden sonra
    /// veri anketi bastırır. Ankette başla, gerçeğe doğru sön.
    /// </summary>
    public const int SurveyPseudoCount = 4;

    /// <summary>
    /// Unutma katsayısı; her güncellemede <c>α ← 1 + f·(α − 1)</c>.
    /// Üç işi birden görür: sayaçlar sonsuza büyümez (smallint taşmaz),
    /// posterior sonsuz kesinliğe kilitlenmez (keşif hiç bitmez) ve zevk
    /// değişimi takip edilir.
    /// </summary>
    public const double Forgetting = 0.99;

    /// <summary>
    /// Bir geri bildirimin ilgili sayaca eklediği ağırlık.
    /// Sabit nokta <c>1 + w / (1 − f)</c> olduğu için en büyük ağırlıkta
    /// bile sayaç 201'de durur; <see cref="short"/> için fazlasıyla güvenli.
    /// </summary>
    public static int WeightOf(ContentFeedback feedback) => feedback switch
    {
        ContentFeedback.Liked => 2,
        ContentFeedback.Disliked => 2,
        ContentFeedback.Completed => 1,
        _ => throw new ArgumentOutOfRangeException(
            nameof(feedback), feedback, null)
    };

    /// <summary>
    /// Unutmayı uygular: <c>α ← 1 + f·(α − 1)</c>. Bir güncellemede
    /// <b>her iki</b> sayaca birden uygulanmalıdır — yalnızca artan sayaç
    /// eskitilirse eski beğeniler hiç sönmez ve zevk değişimi takip
    /// edilemez.
    /// </summary>
    public static short Forget(short count) =>
        Clamp(1 + (Forgetting * (count - 1)));

    /// <summary>Geri bildirimin ağırlığını ekler.</summary>
    public static short Add(short count, int weight) =>
        Clamp(count + weight);

    /// <summary>
    /// Sayaçların anlamlı alt sınırı 1'dir: Beta parametresi 0 olamaz.
    /// </summary>
    private static short Clamp(double count) => (short)Math.Clamp(
        Math.Round(count), UniformCount, short.MaxValue);

    /// <summary>
    /// Ankete verilen cevabın sayaç karşılığı:
    /// <c>α = 1 + s·C</c>, <c>β = 1 + (1 − s)·C</c>.
    /// </summary>
    /// <param name="score">
    /// 1 = sevdim, 0 = sevmedim. Ankette hiç geçmeyen etiket için bu
    /// fonksiyon çağrılmaz: bilgi yoksa Beta(1,1) kalır.
    /// </param>
    public static (short Alpha, short Beta) PriorOf(double score)
    {
        var alpha = UniformCount + (score * SurveyPseudoCount);
        var beta = UniformCount + ((1 - score) * SurveyPseudoCount);

        return ((short)Math.Round(alpha), (short)Math.Round(beta));
    }
}
