using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SereneCycle.Application.Auth;
using SereneCycle.Application.Profiles;

namespace SereneCycle.Api.Controllers;

[Route("me")]
[Authorize]
public class MeController(IProfileService profileService) : ApiControllerBase
{
    /// <summary>Giriş yapmış kullanıcının profili ve döngü ayarları.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(UserSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserSummary>> Get(
        CancellationToken cancellationToken)
    {
        var result =
            await profileService.GetAsync(CurrentUserId, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : Problem(result);
    }

    /// <summary>
    /// Profil ve döngü ayarlarını günceller. Onboarding sihirbazı da bunu
    /// kullanır: <c>lastPeriodStart</c> gönderilirse ilk döngü kaydı açılır.
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(UserSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserSummary>> Update(
        UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await profileService.UpdateAsync(
            CurrentUserId, request, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : Problem(result);
    }
}
