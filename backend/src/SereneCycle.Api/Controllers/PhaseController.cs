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
        CancellationToken cancellationToken) =>
        OkOrProblem(
            await phaseService.GetTodayAsync(CurrentUserId, cancellationToken));

    /// <summary>
    /// Aylık takvim: verilen ayın her günü için faz, adet günü tahmini ve
    /// kullanıcının kaydettiği kanama/lekelenme işaretleri.
    /// </summary>
    [HttpGet("calendar")]
    [ProducesResponseType(
        typeof(CalendarMonthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CalendarMonthResponse>> GetCalendar(
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken cancellationToken) =>
        OkOrProblem(await phaseService.GetMonthAsync(
            CurrentUserId, year, month, cancellationToken));
}
