using SereneCycle.Application.Common;
using SereneCycle.Domain.Content;
using SereneCycle.Domain.Cycles;
using SereneCycle.Domain.Entities;

namespace SereneCycle.Application.Content;

/// <param name="DurationMinutes">
/// Yaklaşık süre; yalnızca egzersizlerde dolu.
/// </param>
public sealed record ContentItemDto(
    int Id,
    CyclePhase Phase,
    ContentType Type,
    string Title,
    string Body,
    int? DurationMinutes);

/// <summary>
/// Beslenme/Hareket ekranlarının ihtiyacı: bir faz için "öncelik ver" ve
/// "sınırlı tüket" listeleri tek çağrıda.
/// </summary>
public sealed record PhaseContentResponse(
    CyclePhase Phase,
    string PhaseName,
    IReadOnlyList<ContentItemDto> Recommended,
    IReadOnlyList<ContentItemDto> Limited,
    string Disclaimer);

public interface IContentService
{
    /// <summary>
    /// Beslenme içeriği. <paramref name="phase"/> verilmezse kullanıcının
    /// bugünkü fazı kullanılır.
    /// </summary>
    Task<Result<PhaseContentResponse>> GetNutritionAsync(
        Guid userId,
        CyclePhase? phase = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Hareket içeriği; faz mantığı beslenmeyle aynı.
    /// </summary>
    /// <param name="availableMinutes">
    /// Kullanıcının o an ayırabildiği süre. Verilirse daha uzun süren
    /// egzersizler aday kümesine hiç girmez — bu öğrenilecek bir tercih
    /// değil, bilinen bir kısıt.
    /// </param>
    Task<Result<PhaseContentResponse>> GetExerciseAsync(
        Guid userId,
        CyclePhase? phase = null,
        int? availableMinutes = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Onboarding anketinin sonucu. Ankette hangi sorunun nereye gittiği
/// bilinçli olarak ayrılmıştır: "Vejetaryen misin?" bir zevk prior'ı değil,
/// <see cref="AvoidFlags"/>'e giden bir kısıttır; "Yoga sever misin?" ise
/// zevk prior'ıdır.
/// </summary>
public sealed record TastePreferencesRequest(
    IReadOnlyList<TasteTag>? LikedTags,
    IReadOnlyList<TasteTag>? DislikedTags,
    IReadOnlyList<ContraTag>? AvoidFlags);

/// <summary>
/// Tek bir öneriye verilen tepki. <c>liked</c> gönderilmeyip
/// <c>completed</c> gönderilirse yalnızca "yaptım" sinyali işlenir.
/// </summary>
public sealed record ContentFeedbackRequest(
    bool? Liked = null,
    bool Completed = false);

/// <summary>
/// Zevk profilinin yazma yolu. Okuma yolundan (<see cref="IContentService"/>)
/// ayrı tutuldu: öneri listesi üretmek hiçbir zaman yazma yapmaz.
/// </summary>
public interface ITasteProfileService
{
    /// <summary>
    /// Anketi kısıt maskesine ve zevk prior'ına çevirir. Anket yeniden
    /// gönderilirse zevk vektörü baştan kurulur.
    /// </summary>
    Task<Result> SavePreferencesAsync(
        Guid userId,
        TastePreferencesRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 👍/👎/"tamamladım" sinyalini öğenin bütün etiketlerine dağıtır.
    /// </summary>
    Task<Result> RecordFeedbackAsync(
        Guid userId,
        int contentItemId,
        ContentFeedbackRequest request,
        CancellationToken cancellationToken = default);
}
