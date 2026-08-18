using System.ComponentModel.DataAnnotations;

namespace SereneCycle.Application.Auth;

public sealed record RegisterRequest(
    [Required, StringLength(60, MinimumLength = 1)] string Name,
    [Required, EmailAddress] string Email,
    [Required, StringLength(100, MinimumLength = 8)] string Password);

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public sealed record VerifyCodeRequest(
    [Required, EmailAddress] string Email,
    [Required, StringLength(6, MinimumLength = 6)] string Code);

public sealed record ResendCodeRequest(
    [Required, EmailAddress] string Email);

public sealed record ForgotPasswordRequest(
    [Required, EmailAddress] string Email);

public sealed record ResetPasswordRequest(
    [Required, EmailAddress] string Email,
    [Required, StringLength(6, MinimumLength = 6)] string Code,
    [Required, StringLength(100, MinimumLength = 8)] string NewPassword);

public sealed record RefreshTokenRequest(
    [Required] string RefreshToken);

/// <summary>Başarılı giriş/doğrulama sonrası dönen token çifti.</summary>
public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    UserSummary User);

/// <param name="AvatarUpdatedAt">
/// Profil fotoğrafı yoksa null. Mobil taraf bunu önbellek anahtarı olarak
/// kullanır: değer değişmedikçe fotoğrafı yeniden indirmez.
/// </param>
public sealed record UserSummary(
    Guid Id,
    string Name,
    string Email,
    bool EmailConfirmed,
    int AvgCycleLength,
    int AvgPeriodLength,
    bool HasCompletedOnboarding,
    DateTimeOffset? AvatarUpdatedAt = null);
