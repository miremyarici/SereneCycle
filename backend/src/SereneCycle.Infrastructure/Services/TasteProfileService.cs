using Microsoft.EntityFrameworkCore;
using SereneCycle.Application.Common;
using SereneCycle.Application.Content;
using SereneCycle.Domain.Content;
using SereneCycle.Domain.Entities;
using SereneCycle.Infrastructure.Content;
using SereneCycle.Infrastructure.Persistence;

namespace SereneCycle.Infrastructure.Services;

/// <summary>
/// Zevk profilinin yazma yolu — <c>RiskSummaryUpdater</c> ile aynı ayrım:
/// okuma tarafı hiçbir zaman yazmaz, yazma tarafı seyrektir.
///
/// İki farklı sinyali iki farklı yere yazar: kesin bilinen kısıtlar
/// kullanıcının <c>AvoidMask</c>'ine (sert filtre), zevk cevapları Beta
/// sayaçlarına (öğrenilen kol).
/// </summary>
public class TasteProfileService(AppDbContext db, ContentCatalog catalog)
    : ITasteProfileService
{
    public async Task<Result> SavePreferencesAsync(
        Guid userId,
        TastePreferencesRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(ErrorCode.NotFound, ServiceErrors.UserNotFound);
        }

        user.AvoidMask = ContraTags.Of(request.AvoidFlags ?? []);

        var profile = await GetOrCreateProfileAsync(userId, cancellationToken);

        profile.ApplySurvey(
            request.LikedTags ?? [],
            request.DislikedTags ?? []);

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> RecordFeedbackAsync(
        Guid userId,
        int contentItemId,
        ContentFeedbackRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (ToFeedback(request) is not { } feedback)
        {
            return Result.Failure(
                ErrorCode.Validation,
                "Geri bildirim boş olamaz: 'liked' ya da 'completed' gönder.");
        }

        var item = await catalog.FindAsync(contentItemId, cancellationToken);

        if (item is null)
        {
            return Result.Failure(ErrorCode.NotFound, "İçerik bulunamadı.");
        }

        var profile = await GetOrCreateProfileAsync(userId, cancellationToken);

        profile.ApplyFeedback(item.TagMask, feedback);

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static ContentFeedback? ToFeedback(ContentFeedbackRequest request) =>
        request switch
        {
            { Liked: true } => ContentFeedback.Liked,
            { Liked: false } => ContentFeedback.Disliked,
            { Completed: true } => ContentFeedback.Completed,
            _ => null
        };

    /// <summary>
    /// Satır ilk yazmada açılır. Okuma yolu satırı olmayan kullanıcı için
    /// geçici bir düzgün dağılım profili kullanır, bu yüzden burada
    /// oluşturmak bir gecikme değil, ilk gerçek sinyalin kaydıdır.
    /// </summary>
    private async Task<UserTasteProfile> GetOrCreateProfileAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var profile = await db.UserTasteProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (profile is not null)
        {
            return profile;
        }

        profile = UserTasteProfile.CreateFor(userId);
        db.UserTasteProfiles.Add(profile);

        return profile;
    }
}
