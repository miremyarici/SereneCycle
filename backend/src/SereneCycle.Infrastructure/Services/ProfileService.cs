using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SereneCycle.Application.Abstractions;
using SereneCycle.Application.Auth;
using SereneCycle.Application.Common;
using SereneCycle.Application.Profiles;
using SereneCycle.Infrastructure.Identity;
using SereneCycle.Infrastructure.Persistence;

namespace SereneCycle.Infrastructure.Services;

public class ProfileService(
    UserManager<AppUser> userManager,
    AppDbContext db,
    CycleRegistrar cycleRegistrar,
    IEmailSender emailSender) : IProfileService
{
    /// <summary>
    /// Fotoğraf satır içinde saklandığı için üst sınır dar tutuldu; mobil
    /// taraf zaten yüklemeden önce kareye kırpıp küçültüyor.
    /// </summary>
    private const int MaxAvatarBytes = 2 * 1024 * 1024;

    private static readonly string[] AllowedAvatarTypes =
        ["image/jpeg", "image/png", "image/webp"];

    public async Task<Result<UserSummary>> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Salt okuma: varlığı izlemeye ve avatar baytlarını (2 MB'a kadar)
        // ağa çıkarmaya gerek yok, doğrudan DTO'ya projeksiyon yeter.
        var summary = await db.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(UserSummaryFactory.Projection)
            .FirstOrDefaultAsync(cancellationToken);

        if (summary is null)
        {
            return Result<UserSummary>.Failure(
                ErrorCode.NotFound, ServiceErrors.UserNotFound);
        }

        return Result<UserSummary>.Success(summary with
        {
            HasCompletedOnboarding =
                await HasAnyCycleAsync(userId, cancellationToken)
        });
    }

    public async Task<Result<UserSummary>> UpdateAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return Result<UserSummary>.Failure(
                ErrorCode.NotFound, ServiceErrors.UserNotFound);
        }

        if (request.Name is not null)
        {
            user.Name = request.Name.Trim();
        }

        if (request.AvgCycleLength is not null)
        {
            user.AvgCycleLength = request.AvgCycleLength.Value;
        }

        if (request.AvgPeriodLength is not null)
        {
            user.AvgPeriodLength = request.AvgPeriodLength.Value;
        }

        var updated = await userManager.UpdateAsync(user);

        if (!updated.Succeeded)
        {
            return Result<UserSummary>.Failure(
                ErrorCode.Validation,
                string.Join(" ", updated.Errors.Select(e => e.Description)));
        }

        // Onboarding sihirbazı son adet tarihini de gönderir: ilk döngüyü açar.
        if (request.LastPeriodStart is { } startDate)
        {
            var result = await cycleRegistrar.StartCycleAsync(
                user, startDate, cancellationToken);

            if (result.IsFailure)
            {
                return Result<UserSummary>.Failure(
                    result.ErrorCode, result.Error!);
            }
        }

        return Result<UserSummary>.Success(
            await ToSummaryAsync(user, cancellationToken));
    }

    // --- Profil fotoğrafı --------------------------------------------------

    public async Task<Result<AvatarContent>> GetAvatarAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var avatar = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => new { u.AvatarData, u.AvatarContentType })
            .FirstOrDefaultAsync(cancellationToken);

        if (avatar?.AvatarData is null || avatar.AvatarContentType is null)
        {
            return Result<AvatarContent>.Failure(
                ErrorCode.NotFound, "Profil fotoğrafı yok.");
        }

        return Result<AvatarContent>.Success(
            new AvatarContent(avatar.AvatarData, avatar.AvatarContentType));
    }

    public async Task<Result<UserSummary>> UpdateAvatarAsync(
        Guid userId,
        UpdateAvatarRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return Result<UserSummary>.Failure(
                ErrorCode.NotFound, ServiceErrors.UserNotFound);
        }

        var contentType = request.ContentType.Trim().ToLowerInvariant();

        if (!AllowedAvatarTypes.Contains(contentType))
        {
            return Result<UserSummary>.Failure(
                ErrorCode.Validation,
                "Yalnızca JPEG, PNG ve WebP fotoğraflar yüklenebilir.");
        }

        byte[] bytes;

        try
        {
            bytes = Convert.FromBase64String(request.Data);
        }
        catch (FormatException)
        {
            return Result<UserSummary>.Failure(
                ErrorCode.Validation, "Fotoğraf okunamadı.");
        }

        if (bytes.Length == 0)
        {
            return Result<UserSummary>.Failure(
                ErrorCode.Validation, "Fotoğraf boş.");
        }

        if (bytes.Length > MaxAvatarBytes)
        {
            return Result<UserSummary>.Failure(
                ErrorCode.Validation, "Fotoğraf 2 MB'tan büyük olamaz.");
        }

        user.AvatarData = bytes;
        user.AvatarContentType = contentType;
        user.AvatarUpdatedAt = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(user);

        return Result<UserSummary>.Success(
            await ToSummaryAsync(user, cancellationToken));
    }

    public async Task<Result<UserSummary>> RemoveAvatarAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return Result<UserSummary>.Failure(
                ErrorCode.NotFound, ServiceErrors.UserNotFound);
        }

        user.AvatarData = null;
        user.AvatarContentType = null;
        user.AvatarUpdatedAt = null;
        await userManager.UpdateAsync(user);

        return Result<UserSummary>.Success(
            await ToSummaryAsync(user, cancellationToken));
    }

    // --- E-posta değişikliği ----------------------------------------------

    public async Task<Result> RequestEmailChangeAsync(
        Guid userId,
        ChangeEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return Result.Failure(ErrorCode.NotFound, ServiceErrors.UserNotFound);
        }

        if (!await userManager.CheckPasswordAsync(user, request.CurrentPassword))
        {
            return Result.Failure(ErrorCode.Unauthorized, "Şifre hatalı.");
        }

        var newEmail = request.NewEmail.Trim().ToLowerInvariant();

        if (newEmail == user.Email?.ToLowerInvariant())
        {
            return Result.Failure(
                ErrorCode.Validation, "Bu zaten mevcut e-posta adresin.");
        }

        var taken = await userManager.FindByEmailAsync(newEmail);

        if (taken is not null)
        {
            return Result.Failure(
                ErrorCode.Conflict, "Bu e-posta zaten kayıtlı.");
        }

        var code = VerificationCodeGenerator.Generate();

        user.PendingEmail = newEmail;
        user.EmailChangeCodeHash = VerificationCodeGenerator.Hash(code);
        user.EmailChangeCodeExpiresAt =
            DateTimeOffset.UtcNow.Add(VerificationCodeGenerator.Lifetime);
        user.EmailChangeAttemptCount = 0;
        await userManager.UpdateAsync(user);

        // Kod yeni adrese gider: adresin gerçekten kullanıcıya ait olduğu
        // ancak orada okunabildiğinde kanıtlanır.
        await emailSender.SendAsync(
            newEmail,
            "Yeni e-posta adresini doğrula",
            EmailTemplates.EmailChangeCode(user.Name, code),
            cancellationToken);

        return Result.Success();
    }

    public async Task<Result<UserSummary>> ConfirmEmailChangeAsync(
        Guid userId,
        ConfirmEmailChangeRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return Result<UserSummary>.Failure(
                ErrorCode.NotFound, ServiceErrors.UserNotFound);
        }

        if (user.PendingEmail is null
            || user.EmailChangeCodeHash is null
            || user.EmailChangeCodeExpiresAt is null
            || user.EmailChangeCodeExpiresAt < DateTimeOffset.UtcNow)
        {
            return Result<UserSummary>.Failure(
                ErrorCode.Validation, "Kod geçersiz veya süresi dolmuş.");
        }

        if (user.EmailChangeAttemptCount
            >= VerificationCodeGenerator.MaxAttempts)
        {
            return Result<UserSummary>.Failure(
                ErrorCode.Validation,
                "Çok fazla hatalı deneme yapıldı. Yeni bir kod iste.");
        }

        if (!VerificationCodeGenerator.Verify(
                request.Code, user.EmailChangeCodeHash))
        {
            user.EmailChangeAttemptCount++;
            await userManager.UpdateAsync(user);

            return Result<UserSummary>.Failure(
                ErrorCode.Validation, "Kod geçersiz veya süresi dolmuş.");
        }

        var newEmail = user.PendingEmail;

        // UserName da e-posta: giriş ekranı yeni adresi kabul etsin.
        // Normalize edilmiş kopyaları UserManager.UpdateAsync kendisi tazeler.
        user.Email = newEmail;
        user.UserName = newEmail;
        user.EmailConfirmed = true;
        user.PendingEmail = null;
        user.EmailChangeCodeHash = null;
        user.EmailChangeCodeExpiresAt = null;
        user.EmailChangeAttemptCount = 0;

        var updated = await userManager.UpdateAsync(user);

        if (!updated.Succeeded)
        {
            return Result<UserSummary>.Failure(
                ErrorCode.Validation,
                string.Join(" ", updated.Errors.Select(e => e.Description)));
        }

        return Result<UserSummary>.Success(
            await ToSummaryAsync(user, cancellationToken));
    }

    // --- Şifre değişikliği -------------------------------------------------

    public async Task<Result> ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return Result.Failure(ErrorCode.NotFound, ServiceErrors.UserNotFound);
        }

        var changed = await userManager.ChangePasswordAsync(
            user, request.CurrentPassword, request.NewPassword);

        if (!changed.Succeeded)
        {
            // Identity yanlış mevcut şifre için "PasswordMismatch" döner;
            // mesajı Türkçeleştirip diğer doğrulama hatalarını olduğu gibi
            // geçiriyoruz.
            var mismatch = changed.Errors.Any(
                e => e.Code == "PasswordMismatch");

            return Result.Failure(
                mismatch ? ErrorCode.Unauthorized : ErrorCode.Validation,
                mismatch
                    ? "Mevcut şifren hatalı."
                    : string.Join(
                        " ", changed.Errors.Select(e => e.Description)));
        }

        // Şifre değişti: kayıtlı bütün oturumlar düşsün. Mevcut erişim
        // token'ı süresi dolana kadar çalışmaya devam eder, yenilenemez.
        await db.RevokeAllForUserAsync(userId, cancellationToken);

        return Result.Success();
    }

    private async Task<UserSummary> ToSummaryAsync(
        AppUser user,
        CancellationToken cancellationToken) =>
        UserSummaryFactory.From(
            user, await HasAnyCycleAsync(user.Id, cancellationToken));

    /// <summary>Onboarding tamamlandı mı: ilk döngü kaydı açıldı mı.</summary>
    private Task<bool> HasAnyCycleAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        db.Cycles.AnyAsync(c => c.UserId == userId, cancellationToken);
}
