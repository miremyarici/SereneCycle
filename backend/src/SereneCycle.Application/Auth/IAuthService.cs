using SereneCycle.Application.Common;

namespace SereneCycle.Application.Auth;

public interface IAuthService
{
    /// <summary>
    /// Hesap oluşturur ve e-postaya 6 haneli doğrulama kodu gönderir.
    /// Kod doğrulanana kadar giriş yapılamaz.
    /// </summary>
    Task<Result> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<AuthResponse>> VerifyCodeAsync(
        VerifyCodeRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> ResendVerificationCodeAsync(
        ResendCodeRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sıfırlama kodu gönderir. Hesap sayımını (account enumeration)
    /// engellemek için e-posta kayıtlı olmasa da başarı döner.
    /// </summary>
    Task<Result> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<AuthResponse>> RefreshTokenAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default);
}
