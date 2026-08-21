using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SereneCycle.Application.Auth;
using SereneCycle.Application.Privacy;
using SereneCycle.Application.Profiles;

namespace SereneCycle.Api.Controllers;

[Route("me")]
[Authorize]
public class MeController(
    IProfileService profileService,
    IAccountDataService accountDataService) : ApiControllerBase
{
    /// <summary>Giriş yapmış kullanıcının profili ve döngü ayarları.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(UserSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserSummary>> Get(
        CancellationToken cancellationToken) =>
        OkOrProblem(
            await profileService.GetAsync(CurrentUserId, cancellationToken));

    /// <summary>
    /// Profil ve döngü ayarlarını günceller. Onboarding sihirbazı da bunu
    /// kullanır: <c>lastPeriodStart</c> gönderilirse ilk döngü kaydı açılır.
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(UserSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserSummary>> Update(
        UpdateProfileRequest request,
        CancellationToken cancellationToken) =>
        OkOrProblem(await profileService.UpdateAsync(
            CurrentUserId, request, cancellationToken));

    /// <summary>
    /// Hesabı ve ona bağlı bütün veriyi kalıcı olarak siler. Geri alınamaz,
    /// bu yüzden mevcut şifre istenir.
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Delete(
        DeleteAccountRequest request,
        CancellationToken cancellationToken) =>
        NoContentOrProblem(await accountDataService.DeleteAsync(
            CurrentUserId, request, cancellationToken));

    /// <summary>
    /// KVKK veri taşınabilirliği: kullanıcının bütün verisi tek bir JSON
    /// belgesinde. Profil fotoğrafı dahil değildir, <c>GET /me/avatar</c>
    /// ile ayrıca indirilir.
    /// </summary>
    [HttpGet("export")]
    [ProducesResponseType(typeof(UserDataExport), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDataExport>> Export(
        CancellationToken cancellationToken) =>
        OkOrProblem(await accountDataService.ExportAsync(
            CurrentUserId, cancellationToken));

    // --- Profil fotoğrafı --------------------------------------------------

    /// <summary>
    /// Profil fotoğrafının ham hâli. Token gerektirdiği için istemci
    /// doğrudan <c>Image.network</c> ile değil, API istemcisiyle indirir.
    /// </summary>
    [HttpGet("avatar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetAvatar(
        CancellationToken cancellationToken)
    {
        var result =
            await profileService.GetAvatarAsync(CurrentUserId, cancellationToken);

        // Tek istisna: gövde JSON değil ham bayt, bu yüzden ortak
        // OkOrProblem dönüşümü kullanılamıyor.
        return result.IsSuccess
            ? File(result.Value!.Data, result.Value.ContentType)
            : Problem(result);
    }

    [HttpPut("avatar")]
    [ProducesResponseType(typeof(UserSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserSummary>> UpdateAvatar(
        UpdateAvatarRequest request,
        CancellationToken cancellationToken) =>
        OkOrProblem(await profileService.UpdateAvatarAsync(
            CurrentUserId, request, cancellationToken));

    [HttpDelete("avatar")]
    [ProducesResponseType(typeof(UserSummary), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserSummary>> RemoveAvatar(
        CancellationToken cancellationToken) =>
        OkOrProblem(await profileService.RemoveAvatarAsync(
            CurrentUserId, cancellationToken));

    // --- E-posta ve şifre --------------------------------------------------

    /// <summary>
    /// Yeni adrese doğrulama kodu gönderir. Adres kod onaylanana kadar
    /// değişmez, bu yüzden yanıt yalnızca "kod gönderildi" anlamına gelir.
    /// </summary>
    [HttpPost("email/change")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> RequestEmailChange(
        ChangeEmailRequest request,
        CancellationToken cancellationToken) =>
        NoContentOrProblem(await profileService.RequestEmailChangeAsync(
            CurrentUserId, request, cancellationToken));

    [HttpPost("email/confirm")]
    [ProducesResponseType(typeof(UserSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserSummary>> ConfirmEmailChange(
        ConfirmEmailChangeRequest request,
        CancellationToken cancellationToken) =>
        OkOrProblem(await profileService.ConfirmEmailChangeAsync(
            CurrentUserId, request, cancellationToken));

    [HttpPost("password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> ChangePassword(
        ChangePasswordRequest request,
        CancellationToken cancellationToken) =>
        NoContentOrProblem(await profileService.ChangePasswordAsync(
            CurrentUserId, request, cancellationToken));
}
