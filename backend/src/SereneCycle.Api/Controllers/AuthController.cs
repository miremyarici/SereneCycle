using Microsoft.AspNetCore.Mvc;
using SereneCycle.Application.Auth;

namespace SereneCycle.Api.Controllers;

[Route("auth")]
public class AuthController(IAuthService authService) : ApiControllerBase
{
    /// <summary>
    /// Hesap oluşturur ve e-postaya 6 haneli doğrulama kodu gönderir.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Register(
        RegisterRequest request,
        CancellationToken cancellationToken) =>
        NoContentOrProblem(
            await authService.RegisterAsync(request, cancellationToken));

    /// <summary>Doğrulama kodunu kontrol eder ve oturum açar.</summary>
    [HttpPost("verify-code")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> VerifyCode(
        VerifyCodeRequest request,
        CancellationToken cancellationToken) =>
        OkOrProblem(
            await authService.VerifyCodeAsync(request, cancellationToken));

    /// <summary>Doğrulama kodunu yeniden gönderir.</summary>
    [HttpPost("resend-code")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> ResendCode(
        ResendCodeRequest request,
        CancellationToken cancellationToken) =>
        NoContentOrProblem(await authService.ResendVerificationCodeAsync(
            request, cancellationToken));

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken) =>
        OkOrProblem(await authService.LoginAsync(request, cancellationToken));

    /// <summary>
    /// Şifre sıfırlama kodu gönderir. E-posta kayıtlı olmasa da 204 döner —
    /// hangi adreslerin kayıtlı olduğu dışarı sızmasın diye.
    /// </summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> ForgotPassword(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken) =>
        NoContentOrProblem(
            await authService.ForgotPasswordAsync(request, cancellationToken));

    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ResetPassword(
        ResetPasswordRequest request,
        CancellationToken cancellationToken) =>
        NoContentOrProblem(
            await authService.ResetPasswordAsync(request, cancellationToken));

    /// <summary>
    /// Refresh token ile yeni token çifti alır. Kullanılan refresh token
    /// iptal edilir (rotation).
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh(
        RefreshTokenRequest request,
        CancellationToken cancellationToken) =>
        OkOrProblem(
            await authService.RefreshTokenAsync(request, cancellationToken));
}
