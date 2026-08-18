using SereneCycle.Application.Common;
using SereneCycle.Domain.Cycles;
using SereneCycle.Domain.Entities;

namespace SereneCycle.Application.Content;

public sealed record ContentItemDto(
    int Id,
    CyclePhase Phase,
    ContentType Type,
    string Title,
    string Body);

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

    /// <summary>Hareket içeriği; faz mantığı beslenmeyle aynı.</summary>
    Task<Result<PhaseContentResponse>> GetExerciseAsync(
        Guid userId,
        CyclePhase? phase = null,
        CancellationToken cancellationToken = default);
}
