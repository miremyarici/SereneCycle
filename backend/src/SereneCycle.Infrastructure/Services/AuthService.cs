using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SereneCycle.Application.Abstractions;
using SereneCycle.Application.Auth;
using SereneCycle.Application.Common;
using SereneCycle.Infrastructure.Identity;
using SereneCycle.Infrastructure.Options;
using SereneCycle.Infrastructure.Persistence;

namespace SereneCycle.Infrastructure.Services;

public class AuthService(
    UserManager<AppUser> userManager,
    AppDbContext db,
    ITokenService tokenService,
    IEmailSender emailSender,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<Result> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = Normalize(request.Email);
        var existing = await userManager.FindByEmailAsync(email);

        if (existing is not null)
        {
            // Doğrulanmamış bir hesap varsa kaydı tekrar denemek yerine
            // yeni kod gönderiyoruz; kullanıcı kilitlenmiş hissetmesin.
            if (!existing.EmailConfirmed)
            {
                await IssueVerificationCodeAsync(existing, cancellationToken);
                return Result.Success();
            }

            return Result.Failure(
                ErrorCode.Conflict, "Bu e-posta zaten kayıtlı.");
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = email,
            UserName = email
        };

        var created = await userManager.CreateAsync(user, request.Password);

        if (!created.Succeeded)
        {
            return Result.Failure(
                ErrorCode.Validation,
                string.Join(" ", created.Errors.Select(e => e.Description)));
        }

        await IssueVerificationCodeAsync(user, cancellationToken);
        return Result.Success();
    }

    public async Task<Result<AuthResponse>> VerifyCodeAsync(
        VerifyCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(Normalize(request.Email));

        if (user is null)
        {
            return Result<AuthResponse>.Failure(
                ErrorCode.Validation, "Kod geçersiz veya süresi dolmuş.");
        }

        var check = ValidateCode(user, request.Code);

        if (check.IsFailure)
        {
            await userManager.UpdateAsync(user); // deneme sayacını kalıcı yap
            return Result<AuthResponse>.Failure(check.ErrorCode, check.Error!);
        }

        user.EmailConfirmed = true;
        ClearCode(user);
        await userManager.UpdateAsync(user);

        var response = await IssueTokensAsync(user, cancellationToken);
        return Result<AuthResponse>.Success(response);
    }

    public async Task<Result> ResendVerificationCodeAsync(
        ResendCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(Normalize(request.Email));

        // Hesap sayımını engellemek için her durumda başarı dönüyoruz.
        if (user is not null && !user.EmailConfirmed)
        {
            await IssueVerificationCodeAsync(user, cancellationToken);
        }

        return Result.Success();
    }

    public async Task<Result<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(Normalize(request.Email));

        // Kullanıcı yoksa da şifre kontrolü kadar zaman harcamak yerine
        // aynı genel mesajı döneriz; hangi e-postanın kayıtlı olduğu sızmaz.
        if (user is null
            || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            return Result<AuthResponse>.Failure(
                ErrorCode.Unauthorized, "E-posta veya şifre hatalı.");
        }

        if (!user.EmailConfirmed)
        {
            return Result<AuthResponse>.Failure(
                ErrorCode.Forbidden,
                "Giriş yapmadan önce e-postanı doğrulaman gerekiyor.");
        }

        var response = await IssueTokensAsync(user, cancellationToken);
        return Result<AuthResponse>.Success(response);
    }

    public async Task<Result> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(Normalize(request.Email));

        if (user is not null)
        {
            var code = VerificationCodeGenerator.Generate();
            StoreCode(user, code);
            await userManager.UpdateAsync(user);

            await emailSender.SendAsync(
                user.Email!,
                "Şifre sıfırlama kodun",
                EmailTemplates.PasswordReset(user.Name, code),
                cancellationToken);
        }

        // Kayıtlı olmayan e-posta için de başarı: account enumeration önlenir.
        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(Normalize(request.Email));

        if (user is null)
        {
            return Result.Failure(
                ErrorCode.Validation, "Kod geçersiz veya süresi dolmuş.");
        }

        var check = ValidateCode(user, request.Code);

        if (check.IsFailure)
        {
            await userManager.UpdateAsync(user);
            return check;
        }

        var resetToken =
            await userManager.GeneratePasswordResetTokenAsync(user);
        var reset = await userManager.ResetPasswordAsync(
            user, resetToken, request.NewPassword);

        if (!reset.Succeeded)
        {
            return Result.Failure(
                ErrorCode.Validation,
                string.Join(" ", reset.Errors.Select(e => e.Description)));
        }

        ClearCode(user);

        // Şifre değişti: mevcut bütün oturumlar düşsün.
        await RevokeAllRefreshTokensAsync(user.Id, cancellationToken);
        await userManager.UpdateAsync(user);

        return Result.Success();
    }

    public async Task<Result<AuthResponse>> RefreshTokenAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var hash = VerificationCodeGenerator.Hash(request.RefreshToken);

        var stored = await db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (stored?.User is null || !stored.IsActive)
        {
            return Result<AuthResponse>.Failure(
                ErrorCode.Unauthorized,
                "Oturum geçersiz, lütfen tekrar giriş yap.");
        }

        // Rotation: kullanılan token iptal edilir, yenisi verilir.
        stored.RevokedAt = DateTimeOffset.UtcNow;

        var response = await IssueTokensAsync(stored.User, cancellationToken);
        return Result<AuthResponse>.Success(response);
    }

    // --- Yardımcılar ------------------------------------------------------

    private async Task IssueVerificationCodeAsync(
        AppUser user,
        CancellationToken cancellationToken)
    {
        var code = VerificationCodeGenerator.Generate();
        StoreCode(user, code);
        await userManager.UpdateAsync(user);

        await emailSender.SendAsync(
            user.Email!,
            "Serene Cycle doğrulama kodun",
            EmailTemplates.VerificationCode(user.Name, code),
            cancellationToken);
    }

    private static void StoreCode(AppUser user, string code)
    {
        user.VerificationCodeHash = VerificationCodeGenerator.Hash(code);
        user.VerificationCodeExpiresAt =
            DateTimeOffset.UtcNow.Add(VerificationCodeGenerator.Lifetime);
        user.VerificationAttemptCount = 0;
    }

    private static void ClearCode(AppUser user)
    {
        user.VerificationCodeHash = null;
        user.VerificationCodeExpiresAt = null;
        user.VerificationAttemptCount = 0;
    }

    /// <summary>
    /// Kodun geçerliliğini kontrol eder ve yanlışsa deneme sayacını artırır.
    /// Hata mesajları bilinçli olarak aynı: saldırgan kodun mu yoksa
    /// e-postanın mı yanlış olduğunu ayırt edemesin.
    /// </summary>
    private static Result ValidateCode(AppUser user, string code)
    {
        if (user.VerificationCodeHash is null
            || user.VerificationCodeExpiresAt is null
            || user.VerificationCodeExpiresAt < DateTimeOffset.UtcNow)
        {
            return Result.Failure(
                ErrorCode.Validation, "Kod geçersiz veya süresi dolmuş.");
        }

        if (user.VerificationAttemptCount
            >= VerificationCodeGenerator.MaxAttempts)
        {
            return Result.Failure(
                ErrorCode.Validation,
                "Çok fazla hatalı deneme yapıldı. Yeni bir kod iste.");
        }

        if (!VerificationCodeGenerator.Verify(code, user.VerificationCodeHash))
        {
            user.VerificationAttemptCount++;
            return Result.Failure(
                ErrorCode.Validation, "Kod geçersiz veya süresi dolmuş.");
        }

        return Result.Success();
    }

    private async Task<AuthResponse> IssueTokensAsync(
        AppUser user,
        CancellationToken cancellationToken)
    {
        var (accessToken, expiresAt) =
            tokenService.CreateAccessToken(user.Id, user.Email!);

        var refreshToken = tokenService.CreateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = VerificationCodeGenerator.Hash(refreshToken),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwt.RefreshTokenDays)
        });

        await db.SaveChangesAsync(cancellationToken);

        var hasCycle = await db.Cycles
            .AnyAsync(c => c.UserId == user.Id, cancellationToken);

        return new AuthResponse(
            accessToken,
            expiresAt,
            refreshToken,
            new UserSummary(
                user.Id,
                user.Name,
                user.Email!,
                user.EmailConfirmed,
                user.AvgCycleLength,
                user.AvgPeriodLength,
                HasCompletedOnboarding: hasCycle));
    }

    private async Task RevokeAllRefreshTokensAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(
                s => s.SetProperty(t => t.RevokedAt, DateTimeOffset.UtcNow),
                cancellationToken);
    }

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
