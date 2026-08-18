using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SereneCycle.Application.Common;
using SereneCycle.Application.Phases;
using SereneCycle.Application.Risk;
using SereneCycle.Domain.Cycles;
using SereneCycle.Infrastructure.Identity;
using SereneCycle.Infrastructure.Persistence;

namespace SereneCycle.Infrastructure.Services;

public class PhaseService(
    UserManager<AppUser> userManager,
    AppDbContext db,
    TimeProvider timeProvider,
    IRiskService riskService) : IPhaseService
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

        var firstDay = today.AddDays(-(CalendarStripDays / 2));

        var calendar = await BuildDaysAsync(
            userId,
            user,
            firstDay,
            firstDay.AddDays(CalendarStripDays - 1),
            today,
            cancellationToken);

        var risk = await riskService.GetCardAsync(
            currentCycle, today, cancellationToken);

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
            CalendarStrip: calendar,
            Risk: risk));
    }

    public async Task<Result<CalendarMonthResponse>> GetMonthAsync(
        Guid userId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        if (year is < 2000 or > 2100 || month is < 1 or > 12)
        {
            return Result<CalendarMonthResponse>.Failure(
                ErrorCode.Validation, "Geçersiz ay.");
        }

        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return Result<CalendarMonthResponse>.Failure(
                ErrorCode.NotFound, "Kullanıcı bulunamadı.");
        }

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var firstDay = new DateOnly(year, month, 1);
        var lastDay = firstDay.AddDays(DateTime.DaysInMonth(year, month) - 1);

        var days = await BuildDaysAsync(
            userId, user, firstDay, lastDay, today, cancellationToken);

        return Result<CalendarMonthResponse>.Success(
            new CalendarMonthResponse(year, month, days));
    }

    /// <summary>
    /// [firstDay, lastDay] aralığındaki her gün için fazı, adet günü olup
    /// olmadığını ve kullanıcının kaydından gelen işaretleri toplar.
    /// </summary>
    private async Task<IReadOnlyList<CalendarDay>> BuildDaysAsync(
        Guid userId,
        AppUser user,
        DateOnly firstDay,
        DateOnly lastDay,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var logs = await db.DailyLogs
            .Where(l => l.UserId == userId
                        && l.LogDate >= firstDay
                        && l.LogDate <= lastDay)
            .Select(l => new
            {
                l.LogDate,
                l.HasBleeding,
                l.BloodColor,
                l.HasSpotting
            })
            .ToListAsync(cancellationToken);

        var logsByDate = logs.ToDictionary(l => l.LogDate);

        // Aralık geçmişe uzanabildiği için tek bir "güncel döngü" yetmiyor:
        // her gün, o güne kadar başlamış en son döngüye göre hesaplanır.
        var cycleStarts = await db.Cycles
            .Where(c => c.UserId == userId && c.StartDate <= lastDay)
            .OrderBy(c => c.StartDate)
            .Select(c => c.StartDate)
            .ToListAsync(cancellationToken);

        var days = new List<CalendarDay>(lastDay.DayNumber - firstDay.DayNumber + 1);

        for (var date = firstDay; date <= lastDay; date = date.AddDays(1))
        {
            logsByDate.TryGetValue(date, out var log);

            var cycleStart = cycleStarts.LastOrDefault(s => s <= date);

            // İlk döngüden önceki günlerin verisi yok; faz uydurmak yerine
            // günü nötr gösteriyoruz.
            if (cycleStart == default)
            {
                days.Add(new CalendarDay(
                    Date: date,
                    CycleDay: 0,
                    Phase: CyclePhase.Menstrual,
                    IsToday: date == today,
                    IsPeriodDay: false,
                    HasLog: log is not null,
                    HasBleeding: log?.HasBleeding ?? false,
                    BloodColor: log?.BloodColor,
                    HasSpotting: log?.HasSpotting ?? false));
                continue;
            }

            var prediction = PhaseCalculator.Calculate(
                cycleStart, user.AvgCycleLength, user.AvgPeriodLength, date);

            days.Add(new CalendarDay(
                Date: date,
                CycleDay: prediction.CycleDay,
                Phase: prediction.CurrentPhase,
                IsToday: date == today,
                IsPeriodDay: prediction.CurrentPhase == CyclePhase.Menstrual,
                HasLog: log is not null,
                HasBleeding: log?.HasBleeding ?? false,
                BloodColor: log?.BloodColor,
                HasSpotting: log?.HasSpotting ?? false));
        }

        return days;
    }
}
