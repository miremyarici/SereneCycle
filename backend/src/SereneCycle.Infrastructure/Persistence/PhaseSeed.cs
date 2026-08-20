using SereneCycle.Domain.Content;
using SereneCycle.Domain.Cycles;
using SereneCycle.Domain.Entities;

namespace SereneCycle.Infrastructure.Persistence;

/// <summary>
/// Tek bir fazın kataloğunu kısa satırlarla yazmak için küçük kurucu. Faz
/// dosya başına sabit olduğu için her satırda tekrar edilmez.
///
/// Tür başına ayrı metot var çünkü kurallar türe göre değişiyor ve bu
/// kuralların veri dosyalarında unutulması mümkün olmasın:
/// <list type="bullet">
///   <item>Öneriler etiket almak <b>zorundadır</b> — etiketsiz öğe hep
///   nötr skorlanır, yani ne ankete ne geri bildirime tepki verir.</item>
///   <item>Egzersizde süre <b>zorunludur</b> — süre filtresinin tek
///   girdisi o, boş kalırsa öğe hiçbir zaman elenmez.</item>
///   <item>Uyarılara (<c>FoodAvoid</c>, <c>ExerciseAvoid</c>) bilinçli
///   olarak kısıt işlenmez: bir uyarının hamile ya da sakatlıklı
///   kullanıcıdan gizlenmesi tam ters etkiyi yaratırdı.</item>
/// </list>
/// </summary>
internal sealed class PhaseSeed(CyclePhase phase)
{
    /// <summary>"Bu fazda öncelik verebileceklerin" listesine giren yiyecek.</summary>
    public ContentItem Food(
        string title,
        string body,
        TasteTag[] tastes,
        ContraTag[]? contras = null) =>
        Of(ContentType.FoodDo, title, body, tastes, contras);

    /// <summary>"Sınırlı tutabileceklerin" listesine giren yiyecek.</summary>
    public ContentItem LimitFood(
        string title,
        string body,
        TasteTag[]? tastes = null) =>
        Of(ContentType.FoodAvoid, title, body, tastes ?? [], contras: null);

    /// <param name="minutes">
    /// Yaklaşık süre. Süre filtresi buna bakar; "10 dakikam var" diyen
    /// kullanıcıya daha uzun hiçbir hareket gösterilmez.
    /// </param>
    public ContentItem Exercise(
        string title,
        string body,
        int minutes,
        TasteTag[] tastes,
        ContraTag[]? contras = null) =>
        Of(ContentType.ExerciseDo, title, body, tastes, contras, minutes);

    /// <summary>
    /// "Şimdilik erteleyebileceklerin" listesine giren hareket. Süre
    /// verilmez: uyarı bir plan değil, süre filtresine takılmamalı.
    /// </summary>
    public ContentItem SkipExercise(
        string title,
        string body,
        TasteTag[]? tastes = null) =>
        Of(ContentType.ExerciseAvoid, title, body, tastes ?? [], contras: null);

    private ContentItem Of(
        ContentType type,
        string title,
        string body,
        TasteTag[] tastes,
        ContraTag[]? contras,
        int? durationMinutes = null) =>
        new()
        {
            Phase = phase,
            Type = type,
            Title = title,
            Body = body,
            TagMask = TasteTags.Of(tastes),
            ContraMask = ContraTags.Of(contras ?? []),
            DurationMinutes = durationMinutes
        };
}
