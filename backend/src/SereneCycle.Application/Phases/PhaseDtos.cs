using SereneCycle.Application.Common;
using SereneCycle.Domain.Cycles;

namespace SereneCycle.Application.Phases;

/// <summary>
/// Ana sayfanın tek çağrıda ihtiyaç duyduğu her şey: faz kartı, ilerleme
/// halkası, yatay takvim şeridi ve "bu fazda yaygın duygular" kartı.
/// </summary>
public sealed record PhaseTodayResponse(
    CyclePhase Phase,
    string PhaseName,
    string PhaseDescription,
    int CycleDay,
    int CycleLength,
    DateOnly CycleStartDate,
    DateOnly PredictedNextPeriod,
    DateOnly PredictedOvulation,
    bool IsIrregular,
    bool IsPeriodLate,
    IReadOnlyList<string> CommonMoods,
    IReadOnlyList<CalendarDay> CalendarStrip);

/// <summary>Takvim şeridindeki tek bir gün.</summary>
public sealed record CalendarDay(
    DateOnly Date,
    int CycleDay,
    CyclePhase Phase,
    bool IsToday,
    bool IsPeriodDay,
    bool HasLog);

public interface IPhaseService
{
    /// <summary>
    /// Kullanıcının bugünkü faz bilgisini döner. Hiç döngü kaydı yoksa
    /// (onboarding tamamlanmamışsa) <see cref="ErrorCode.NotFound"/> döner.
    /// </summary>
    Task<Result<PhaseTodayResponse>> GetTodayAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
