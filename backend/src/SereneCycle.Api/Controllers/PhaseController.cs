using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SereneCycle.Application.Phases;

namespace SereneCycle.Api.Controllers;

[Route("phase")]
[Authorize]
public class PhaseController(IPhaseService phaseService) : ApiControllerBase
{
    /// <summary>
    /// Ana sayfanın tek çağrıda ihtiyaç duyduğu her şey: bugünkü faz,
    /// döngü ilerlemesi, tahminler, yaygın duygular ve 7 günlük takvim şeridi.
    /// Onboarding tamamlanmamışsa 404 döner.
    /// </summary>
    [HttpGet("today")]
    [ProducesResponseType(typeof(PhaseTodayResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PhaseTodayResponse>> GetToday(
        CancellationToken cancellationToken)
    {
        var result =
            await phaseService.GetTodayAsync(CurrentUserId, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : Problem(result);
    }
}
