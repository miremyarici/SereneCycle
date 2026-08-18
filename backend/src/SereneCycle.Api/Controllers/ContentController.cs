using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SereneCycle.Application.Content;
using SereneCycle.Domain.Cycles;

namespace SereneCycle.Api.Controllers;

[Route("content")]
[Authorize]
public class ContentController(IContentService contentService)
    : ApiControllerBase
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
        CancellationToken cancellationToken)
    {
        var result = await contentService.GetNutritionAsync(
            CurrentUserId, phase, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : Problem(result);
    }

    /// <summary>Hareket önerileri; faz mantığı beslenmeyle aynı.</summary>
    [HttpGet("exercise")]
    [ProducesResponseType(
        typeof(PhaseContentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PhaseContentResponse>> GetExercise(
        [FromQuery] CyclePhase? phase,
        CancellationToken cancellationToken)
    {
        var result = await contentService.GetExerciseAsync(
            CurrentUserId, phase, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : Problem(result);
    }
}
