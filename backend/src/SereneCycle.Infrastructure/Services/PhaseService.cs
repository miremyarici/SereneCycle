using Microsoft.EntityFrameworkCore;
using SereneCycle.Application.Common;
using SereneCycle.Application.Phases;
using SereneCycle.Application.Risk;
using SereneCycle.Domain.Cycles;
using SereneCycle.Infrastructure.Persistence;

namespace SereneCycle.Infrastructure.Services;

public class PhaseService(
    AppDbContext db,
    TimeProvider timeProvider,
    IRiskService riskService) : IPhaseService
{
    /// <summary>Ana sayfadaki yatay takvim şeridinde gösterilen gün sayısı.</summary>
    private const int CalendarStripDays = 7;

    private const string NoCycleYet =
        "Henüz döngü kaydın yok. Önce onboarding'i tamamla.";

    /// <summary>Takvim ucunun kabul ettiği yıl aralığı.</summary>
    private const int MinCalendarYear = 2000;
    private const int MaxCalendarYear = 2100;

    public async Task<Result<PhaseTodayResponse>> GetTodayAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var today = Today();

        var settings = await FindCycleSettingsAsync(userId, cancellationToken);

        if (settings is null)
        {
            return Result<PhaseTodayResponse>.Failure(
                ErrorCode.NotFound, ServiceErrors.UserNotFound);
        }

        // Bugünü kapsayan en son döngü. Kullanıcı geçmişe dönük bir tarih
        // girmiş olabilir, bu yüzden "en yeni" değil "bugünden önceki en yeni".
        var currentCycle = await db.Cycles
            .Where(c => c.UserId == userId && c.StartDate <= today)
            .OrderByDescending(c => c.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (currentCycle is null)
        {
            return Result<PhaseTodayResponse>.Failure(
                ErrorCode.NotFound, NoCycleYet);
        }

        var prediction = Predict(settings, currentCycle.StartDate, today);

        var firstDay = today.AddDays(-(CalendarStripDays / 2));

        var calendar = await BuildDaysAsync(
            userId,
            settings,
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

    public async Task<Result<CyclePhase>> GetCurrentPhaseAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var today = Today();

        var settings = await FindCycleSettingsAsync(userId, cancellationToken);

        if (settings is null)
        {
            return Result<CyclePhase>.Failure(
                ErrorCode.NotFound, ServiceErrors.UserNotFound);
        }

        // Yalnızca başlangıç tarihi okunur: faz için döngü satırının
        // tamamına da, takvim şeridine de, risk kartına da gerek yok.
        var cycleStart = await db.Cycles
            .Where(c => c.UserId == userId && c.StartDate <= today)
            .OrderByDescending(c => c.StartDate)
            .Select(c => (DateOnly?)c.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        return cycleStart is { } start
            ? Result<CyclePhase>.Success(
                Predict(settings, start, today).CurrentPhase)
            : Result<CyclePhase>.Failure(ErrorCode.NotFound, NoCycleYet);
    }

    public async Task<Result<CalendarMonthResponse>> GetMonthAsync(
        Guid userId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        if (year is < MinCalendarYear or > MaxCalendarYear
            || month is < 1 or > 12)
        {
            return Result<CalendarMonthResponse>.Failure(
                ErrorCode.Validation, "Geçersiz ay.");
        }

        var settings = await FindCycleSettingsAsync(userId, cancellationToken);

        if (settings is null)
        {
            return Result<CalendarMonthResponse>.Failure(
                ErrorCode.NotFound, ServiceErrors.UserNotFound);
        }

        var firstDay = new DateOnly(year, month, 1);
        var lastDay = firstDay.AddDays(DateTime.DaysInMonth(year, month) - 1);

        var days = await BuildDaysAsync(
            userId, settings, firstDay, lastDay, Today(), cancellationToken);

        return Result<CalendarMonthResponse>.Success(
            new CalendarMonthResponse(year, month, days));
    }

    private DateOnly Today() =>
        DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

    /// <summary>
    /// Faz hesabının ihtiyacı olan iki ayar. Kullanıcı satırının tamamı
    /// çekilmiyor: <c>AvatarData</c> satır içinde saklandığı için (2 MB'a
    /// kadar) her ana sayfa açılışında ağa çıkmasının anlamı yok.
    /// </summary>
    private async Task<CycleSettings?> FindCycleSettingsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var settings = await db.Users
            .Where(user => user.Id == userId)
            .Select(user => new
            {
                user.AvgCycleLength,
                user.AvgPeriodLength
            })
            .FirstOrDefaultAsync(cancellationToken);

        return settings is null
            ? null
            : new CycleSettings(
                settings.AvgCycleLength, settings.AvgPeriodLength);
    }

    private static PhasePrediction Predict(
        CycleSettings settings,
        DateOnly cycleStart,
        DateOnly date) =>
        PhaseCalculator.Calculate(
            cycleStart,
            settings.AvgCycleLength,
            settings.AvgPeriodLength,
            date);

    /// <summary>
    /// [firstDay, lastDay] aralığındaki her gün için fazı, adet günü olup
    /// olmadığını ve kullanıcının kaydından gelen işaretleri toplar.
    /// </summary>
    private async Task<IReadOnlyList<CalendarDay>> BuildDaysAsync(
        Guid userId,
        CycleSettings settings,
        DateOnly firstDay,
        DateOnly lastDay,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var logsByDate = await db.DailyLogs
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
            .ToDictionaryAsync(l => l.LogDate, cancellationToken);

        // Aralık geçmişe uzanabildiği için tek bir "güncel döngü" yetmiyor:
        // her gün, o güne kadar başlamış en son döngüye göre hesaplanır.
        var cycleStarts = await db.Cycles
            .Where(c => c.UserId == userId && c.StartDate <= lastDay)
            .OrderBy(c => c.StartDate)
            .Select(c => c.StartDate)
            .ToListAsync(cancellationToken);

        var days =
            new List<CalendarDay>(lastDay.DayNumber - firstDay.DayNumber + 1);

        // Günler de başlangıçlar da artan sırada: listeyi her gün için
        // baştan taramak yerine tek bir imleç ilerletiliyor.
        // O(gün × döngü) → O(gün + döngü).
        var nextCycle = 0;
        DateOnly? activeCycleStart = null;

        for (var date = firstDay; date <= lastDay; date = date.AddDays(1))
        {
            while (nextCycle < cycleStarts.Count
                   && cycleStarts[nextCycle] <= date)
            {
                activeCycleStart = cycleStarts[nextCycle++];
            }

            logsByDate.TryGetValue(date, out var log);

            // İlk döngüden önceki günlerin verisi yok; faz uydurmak yerine
            // günü nötr gösteriyoruz.
            var prediction = activeCycleStart is { } start
                ? Predict(settings, start, date)
                : null;

            days.Add(new CalendarDay(
                Date: date,
                CycleDay: prediction?.CycleDay ?? 0,
                Phase: prediction?.CurrentPhase ?? CyclePhase.Menstrual,
                IsToday: date == today,
                IsPeriodDay: prediction?.CurrentPhase == CyclePhase.Menstrual,
                HasLog: log is not null,
                HasBleeding: log?.HasBleeding ?? false,
                BloodColor: log?.BloodColor,
                HasSpotting: log?.HasSpotting ?? false));
        }

        return days;
    }

    /// <summary>Faz hesabının kullanıcıdan ihtiyaç duyduğu tek şey.</summary>
    private sealed record CycleSettings(int AvgCycleLength, int AvgPeriodLength);
}
