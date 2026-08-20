using System.Collections.Frozen;
using Microsoft.EntityFrameworkCore;
using SereneCycle.Application.Common;
using SereneCycle.Application.Content;
using SereneCycle.Application.Phases;
using SereneCycle.Domain.Content;
using SereneCycle.Domain.Cycles;
using SereneCycle.Domain.Entities;
using SereneCycle.Infrastructure.Content;
using SereneCycle.Infrastructure.Persistence;

namespace SereneCycle.Infrastructure.Services;

/// <summary>
/// Öneri listesinin okuma yolu: kısıtları ve zevk vektörünü toplar, θ'yı
/// istek başına bir kez çeker ve saf seçim fonksiyonunu çalıştırır.
///
/// Yazma yapmaz ve önbellek tutmaz: liste günlük tohumla üretildiği için
/// aynı gün içindeki her çağrı zaten aynı sonucu verir, dolayısıyla
/// önbellek yalnızca geçersizleştirme derdi getirirdi.
///
/// Hiçbir sorguda kullanıcı sayısı ya da kullanıcının geçmiş uzunluğu
/// geçmez: iki tane O(1) birincil anahtar getirisi, gerisi bellekte.
/// </summary>
public class ContentService(
    AppDbContext db,
    ContentCatalog catalog,
    IPhaseService phaseService,
    TimeProvider timeProvider) : IContentService
{
    /// <summary>"Bu fazda öncelik verebileceklerin" listesinin uzunluğu.</summary>
    private const int RecommendedCount = 3;

    /// <summary>"Sınırlı tutabileceklerin" listesi daha kısa tutulur.</summary>
    private const int LimitedCount = 2;

    public Task<Result<PhaseContentResponse>> GetNutritionAsync(
        Guid userId,
        CyclePhase? phase = null,
        CancellationToken cancellationToken = default) =>
        GetAsync(
            userId, phase,
            ContentType.FoodDo, ContentType.FoodAvoid,
            availableMinutes: null,
            cancellationToken);

    public Task<Result<PhaseContentResponse>> GetExerciseAsync(
        Guid userId,
        CyclePhase? phase = null,
        int? availableMinutes = null,
        CancellationToken cancellationToken = default) =>
        GetAsync(
            userId, phase,
            ContentType.ExerciseDo, ContentType.ExerciseAvoid,
            availableMinutes,
            cancellationToken);

    private async Task<Result<PhaseContentResponse>> GetAsync(
        Guid userId,
        CyclePhase? requestedPhase,
        ContentType recommendedType,
        ContentType limitedType,
        int? availableMinutes,
        CancellationToken cancellationToken)
    {
        var resolved = await ResolvePhaseAsync(
            userId, requestedPhase, cancellationToken);

        if (resolved.IsFailure)
        {
            return Result<PhaseContentResponse>.Failure(
                resolved.ErrorCode, resolved.Error!);
        }

        var phase = resolved.Value;
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var avoidMask = await db.Users
            .Where(user => user.Id == userId)
            .Select(user => user.AvoidMask)
            .FirstOrDefaultAsync(cancellationToken);

        var profile = await db.UserTasteProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken)
            ?? UserTasteProfile.CreateFor(userId);

        // θ istek başına bir kez çekilir, öğe başına değil: Thompson
        // örneklemesi tek bir dünya hipotezi çekip ona göre açgözlü
        // davranmaktır. İki liste de aynı hipotezi paylaşır.
        var todayScores = SampleScoresFor(profile, userId, today, phase);
        var previousScores =
            SampleScoresFor(profile, userId, today.AddDays(-1), phase);

        var baseContext = new RecommendationContext
        {
            AvoidMask = avoidMask,
            AvailableMinutes = availableMinutes
        };

        var recommended = await SelectAsync(
            phase, recommendedType,
            baseContext with { Count = RecommendedCount },
            todayScores, previousScores,
            cancellationToken);

        var limited = await SelectAsync(
            phase, limitedType,
            baseContext with { Count = LimitedCount },
            todayScores, previousScores,
            cancellationToken);

        return Result<PhaseContentResponse>.Success(new PhaseContentResponse(
            Phase: phase,
            PhaseName: PhaseContent.NameOf(phase),
            Recommended: ToDtos(recommended),
            Limited: ToDtos(limited),
            Disclaimer: PhaseContent.MedicalDisclaimer));
    }

    /// <summary>Faz verilmediyse kullanıcının bugünkü fazı kullanılır.</summary>
    private Task<Result<CyclePhase>> ResolvePhaseAsync(
        Guid userId,
        CyclePhase? requestedPhase,
        CancellationToken cancellationToken) =>
        requestedPhase is { } phase
            ? Task.FromResult(Result<CyclePhase>.Success(phase))
            : phaseService.GetCurrentPhaseAsync(userId, cancellationToken);

    private async Task<IReadOnlyList<ContentItem>> SelectAsync(
        CyclePhase phase,
        ContentType type,
        RecommendationContext context,
        IReadOnlyList<double> todayScores,
        IReadOnlyList<double> previousScores,
        CancellationToken cancellationToken)
    {
        var candidates =
            await catalog.GetCandidatesAsync(phase, type, cancellationToken);

        // Dün ne gösterildiği saklanmaz, yeniden hesaplanır: günlük tohum
        // sayesinde "dünkü liste" bir kayıt değil, saf bir fonksiyondur.
        // Bu, kullanıcı başına ek satır ve önbellek geçersizleştirme
        // maliyetini tamamen ortadan kaldırır.
        var shownYesterday = Recommender.Select(
            candidates, context, previousScores);

        return Recommender.Select(
            candidates,
            context with
            {
                RecentlyShownIds = shownYesterday
                    .Select(item => item.Id)
                    .ToFrozenSet()
            },
            todayScores);
    }

    private static double[] SampleScoresFor(
        UserTasteProfile profile,
        Guid userId,
        DateOnly date,
        CyclePhase phase) =>
        profile.SampleTasteScores(new Random(DailySeed.Of(userId, date, phase)));

    private static IReadOnlyList<ContentItemDto> ToDtos(
        IReadOnlyList<ContentItem> items) =>
        [.. items.Select(item => new ContentItemDto(
            item.Id, item.Phase, item.Type,
            item.Title, item.Body, item.DurationMinutes))];
}
