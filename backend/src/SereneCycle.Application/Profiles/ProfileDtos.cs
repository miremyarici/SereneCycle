using System.ComponentModel.DataAnnotations;
using SereneCycle.Application.Auth;
using SereneCycle.Application.Common;

namespace SereneCycle.Application.Profiles;

/// <summary>
/// Profil + döngü ayarları güncellemesi. Onboarding sihirbazı da bunu kullanır:
/// <see cref="LastPeriodStart"/> verilirse ilk döngü kaydı açılır.
/// </summary>
public sealed record UpdateProfileRequest(
    [StringLength(60, MinimumLength = 1)] string? Name,
    [Range(21, 45)] int? AvgCycleLength,
    [Range(1, 14)] int? AvgPeriodLength,
    DateOnly? LastPeriodStart);

/// <summary>
/// Profil fotoğrafı yüklemesi. Dosya base64 olarak taşınır: tek bir JSON
/// gövdesi hem mobilde hem Swagger'da denemesi kolay.
/// </summary>
public sealed record UpdateAvatarRequest(
    [Required] string ContentType,
    [Required] string Data);

/// <summary>
/// E-posta değişikliği isteği. Şifre sorulur ki telefonu eline geçiren biri
/// hesabın adresini değiştirip kilitleyemesin.
/// </summary>
public sealed record ChangeEmailRequest(
    [Required, EmailAddress] string NewEmail,
    [Required] string CurrentPassword);

/// <summary>Yeni adrese gönderilen 6 haneli kodun doğrulanması.</summary>
public sealed record ConfirmEmailChangeRequest(
    [Required, StringLength(6, MinimumLength = 6)] string Code);

public sealed record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required, StringLength(100, MinimumLength = 8)] string NewPassword);

/// <summary>Profil fotoğrafının ham hâli; <c>GET /me/avatar</c> döner.</summary>
public sealed record AvatarContent(byte[] Data, string ContentType);

public interface IProfileService
{
    Task<Result<UserSummary>> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Result<UserSummary>> UpdateAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<AvatarContent>> GetAvatarAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Result<UserSummary>> UpdateAvatarAsync(
        Guid userId,
        UpdateAvatarRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<UserSummary>> RemoveAvatarAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Yeni adrese doğrulama kodu gönderir. Adres, kod doğrulanana kadar
    /// değişmez.
    /// </summary>
    Task<Result> RequestEmailChangeAsync(
        Guid userId,
        ChangeEmailRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<UserSummary>> ConfirmEmailChangeAsync(
        Guid userId,
        ConfirmEmailChangeRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default);
}
