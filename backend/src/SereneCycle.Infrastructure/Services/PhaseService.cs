using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SereneCycle.Application.Common;
using SereneCycle.Application.Phases;
using SereneCycle.Domain.Cycles;
using SereneCycle.Infrastructure.Identity;
using SereneCycle.Infrastructure.Persistence;

namespace SereneCycle.Infrastructure.Services;

public class PhaseService(
    UserManager<AppUser> userManager,
    AppDbContext db,
    TimeProvider timeProvider) : IPhaseService
{
    /// <summary>Ana sayfadaki yatay takvim şeridinde gösterilen gün sayısı.</summary>
    private const int CalendarStripDays = 7;

    public async Task<Result<PhaseTodayResponse>> GetTodayAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return Result<PhaseTodayResponse>.Failure(
                ErrorCode.NotFound, "Kullanıcı bulunamadı.");
        }

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        // Bugünü kapsayan en son döngü. Kullanıcı geçmişe dönük bir tarih
        // girmiş olabilir, bu yüzden "en yeni" değil "bugünden önceki en yeni".
        var currentCycle = await db.Cycles
            .Where(c => c.UserId == userId && c.StartDate <= today)
            .OrderByDescending(c => c.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (currentCycle is null)
        {
            return Result<PhaseTodayResponse>.Failure(
                ErrorCode.NotFound,
                "Henüz döngü kaydın yok. Önce onboarding'i tamamla.");
        }

        var prediction = PhaseCalculator.Calculate(
            currentCycle.StartDate,
            user.AvgCycleLength,
            user.AvgPeriodLength,
            today);

        var calendar = await BuildCalendarStripAsync(
            userId, currentCycle.StartDate, user, today, cancellationToken);

        return Result<PhaseTodayResponse>.Success(new PhaseTodayResponse(
            Phase: prediction.CurrentPhase,
            PhaseName: PhaseContent.NameOf(prediction.CurrentPhase),
            PhaseDescription:
                PhaseContent.DescriptionOf(prediction.CurrentPhase),
            CycleDay: prediction.CycleDay,
            CycleLength: prediction.CycleLength,
            CycleStartDate: currentCycle.StartDate,
            PredictedNextPeriod: prediction.PredictedNextPeriod,
            PredictedOvulation: prediction.PredictedOvulation,
            IsIrregular: prediction.IsIrregular,
            IsPeriodLate: prediction.IsPeriodLate,
            CommonMoods: PhaseContent.CommonMoodsOf(prediction.CurrentPhase),
            CalendarStrip: calendar));
    }

    /// <summary>
    /// Bugünün ortada olduğu 7 günlük şerit. Her gün için fazı, adet günü
    /// olup olmadığını ve kullanıcının o güne kayıt girip girmediğini döner.
    /// </summary>
    private async Task<IReadOnlyList<CalendarDay>> BuildCalendarStripAsync(
        Guid userId,
        DateOnly cycleStart,
        AppUser user,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var firstDay = today.AddDays(-(CalendarStripDays / 2));
        var lastDay = firstDay.AddDays(CalendarStripDays - 1);

        var loggedDates = await db.DailyLogs
            .Where(l => l.UserId == userId
                        && l.LogDate >= firstDay
                        && l.LogDate <= lastDay)
            .Select(l => l.LogDate)
            .ToListAsync(cancellationToken);

        var loggedSet = loggedDates.ToHashSet();
        var days = new List<CalendarDay>(CalendarStripDays);

        for (var i = 0; i < CalendarStripDays; i++)
        {
            var date = firstDay.AddDays(i);

            // Döngü başlangıcından önceki günler bir önceki döngüye ait;
            // o döngünün verisi olmayabilir, bu yüzden faz hesaplamayız.
            if (date < cycleStart)
            {
                days.Add(new CalendarDay(
                    Date: date,
                    CycleDay: 0,
                    Phase: CyclePhase.Menstrual,
                    IsToday: date == today,
                    IsPeriodDay: false,
                    HasLog: loggedSet.Contains(date)));
                continue;
            }

            var dayPrediction = PhaseCalculator.Calculate(
                cycleStart, user.AvgCycleLength, user.AvgPeriodLength, date);

            days.Add(new CalendarDay(
                Date: date,
                CycleDay: dayPrediction.CycleDay,
                Phase: dayPrediction.CurrentPhase,
                IsToday: date == today,
                IsPeriodDay: dayPrediction.CurrentPhase == CyclePhase.Menstrual,
                HasLog: loggedSet.Contains(date)));
        }

        return days;
    }
}
