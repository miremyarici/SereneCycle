using Microsoft.EntityFrameworkCore;
using SereneCycle.Application.Common;
using SereneCycle.Application.Content;
using SereneCycle.Application.Phases;
using SereneCycle.Domain.Cycles;
using SereneCycle.Domain.Entities;
using SereneCycle.Infrastructure.Persistence;

namespace SereneCycle.Infrastructure.Services;

public class ContentService(
    AppDbContext db,
    IPhaseService phaseService) : IContentService
{
    public Task<Result<PhaseContentResponse>> GetNutritionAsync(
        Guid userId,
        CyclePhase? phase = null,
        CancellationToken cancellationToken = default) =>
        GetAsync(
            userId, phase,
            ContentType.FoodDo, ContentType.FoodAvoid,
            cancellationToken);

    public Task<Result<PhaseContentResponse>> GetExerciseAsync(
        Guid userId,
        CyclePhase? phase = null,
        CancellationToken cancellationToken = default) =>
        GetAsync(
            userId, phase,
            ContentType.ExerciseDo, ContentType.ExerciseAvoid,
            cancellationToken);

    private async Task<Result<PhaseContentResponse>> GetAsync(
        Guid userId,
        CyclePhase? requestedPhase,
        ContentType recommendedType,
        ContentType limitedType,
        CancellationToken cancellationToken)
    {
        var phase = requestedPhase;

        // Faz verilmediyse kullanıcının bugünkü fazını kullan.
        if (phase is null)
        {
            var today = await phaseService.GetTodayAsync(
                userId, cancellationToken);

            if (today.IsFailure)
            {
                return Result<PhaseContentResponse>.Failure(
                    today.ErrorCode, today.Error!);
            }

            phase = today.Value!.Phase;
        }

        var items = await db.ContentItems
            .Where(c => c.Phase == phase
                        && (c.Type == recommendedType || c.Type == limitedType))
            .OrderBy(c => c.Id)
            .Select(c => new ContentItemDto(
                c.Id, c.Phase, c.Type, c.Title, c.Body))
            .ToListAsync(cancellationToken);

        return Result<PhaseContentResponse>.Success(new PhaseContentResponse(
            Phase: phase.Value,
            PhaseName: PhaseContent.NameOf(phase.Value),
            Recommended: items.Where(i => i.Type == recommendedType).ToList(),
            Limited: items.Where(i => i.Type == limitedType).ToList(),
            Disclaimer: PhaseContent.MedicalDisclaimer));
    }
}
