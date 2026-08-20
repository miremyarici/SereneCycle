using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SereneCycle.Application.Content;
using SereneCycle.Domain.Cycles;

namespace SereneCycle.Api.Controllers;

[Route("content")]
[Authorize]
public class ContentController(
    IContentService contentService,
    ITasteProfileService tasteProfileService) : ApiControllerBase
{
    /// <summary>
    /// Beslenme önerileri. <c>phase</c> verilmezse kullanıcının bugünkü
    /// fazı kullanılır.
    /// </summary>
    [HttpGet("nutrition")]
    [ProducesResponseType(
        typeof(PhaseContentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PhaseContentResponse>> GetNutrition(
        [FromQuery] CyclePhase? phase,
        CancellationToken cancellationToken) =>
        OkOrProblem(await contentService.GetNutritionAsync(
            CurrentUserId, phase, cancellationToken));

    /// <summary>
    /// Hareket önerileri; faz mantığı beslenmeyle aynı. <c>minutes</c>
    /// verilirse daha uzun süren egzersizler hiç önerilmez.
    /// </summary>
    [HttpGet("exercise")]
    [ProducesResponseType(
        typeof(PhaseContentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PhaseContentResponse>> GetExercise(
        [FromQuery] CyclePhase? phase,
        [FromQuery] [Range(1, 240)] int? minutes,
        CancellationToken cancellationToken) =>
        OkOrProblem(await contentService.GetExerciseAsync(
            CurrentUserId, phase, minutes, cancellationToken));

    /// <summary>
    /// Onboarding anketinin sonucu: kısıtlar sert filtreye, zevk cevapları
    /// öğrenme prior'ına yazılır.
    /// </summary>
    [HttpPut("preferences")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> SavePreferences(
        TastePreferencesRequest request,
        CancellationToken cancellationToken) =>
        NoContentOrProblem(await tasteProfileService.SavePreferencesAsync(
            CurrentUserId, request, cancellationToken));

    /// <summary>Tek bir öneriye verilen 👍/👎 ya da "tamamladım" işareti.</summary>
    [HttpPost("{id:int}/feedback")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> SendFeedback(
        int id,
        ContentFeedbackRequest request,
        CancellationToken cancellationToken) =>
        NoContentOrProblem(await tasteProfileService.RecordFeedbackAsync(
            CurrentUserId, id, request, cancellationToken));
}
