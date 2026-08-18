using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SereneCycle.Application.Common;
using SereneCycle.Application.Logs;
using SereneCycle.Domain.Entities;
using SereneCycle.Domain.Risk;
using SereneCycle.Infrastructure.Identity;
using SereneCycle.Infrastructure.Persistence;

namespace SereneCycle.Infrastructure.Services;

public class LogService(
    AppDbContext db,
    UserManager<AppUser> userManager,
    CycleRegistrar cycleRegistrar,
    RiskSummaryUpdater riskSummaryUpdater) : ILogService
{
    public async Task<IReadOnlyList<SymptomOption>> GetSymptomOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var ids = SymptomSeedData.PeriodLogSymptomIds;

        var names = await db.Symptoms
            .Where(s => ids.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        // Sıra seed listesinden gelir; veritabanı sırasına güvenmiyoruz.
        return [.. ids
            .Where(names.ContainsKey)
            .Select(id => new SymptomOption(id, names[id]))];
    }

    public async Task<Result<DailyLogResponse>> GetAsync(
        Guid userId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var log = await db.DailyLogs
            .Include(l => l.LogSymptoms)
            .AsNoTracking()
            .FirstOrDefaultAsync(
                l => l.UserId == userId && l.LogDate == date,
                cancellationToken);

        return Result<DailyLogResponse>.Success(ToResponse(date, log));
    }

    public async Task<Result<DailyLogResponse>> SaveAsync(
        Guid userId,
        DateOnly date,
        SaveDailyLogRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return Result<DailyLogResponse>.Failure(
                ErrorCode.NotFound, "Kullanıcı bulunamadı.");
        }

        var symptomIds = (request.SymptomIds ?? []).Distinct().ToList();

        if (symptomIds.Count > 0)
        {
            var known = await db.Symptoms
                .Where(s => symptomIds.Contains(s.Id))
                .CountAsync(cancellationToken);

            if (known != symptomIds.Count)
            {
                return Result<DailyLogResponse>.Failure(
                    ErrorCode.Validation, "Tanımsız bir belirti gönderildi.");
            }
        }

        var log = await db.DailyLogs
            .Include(l => l.LogSymptoms)
            .FirstOrDefaultAsync(
                l => l.UserId == userId && l.LogDate == date,
                cancellationToken);

        if (log is null)
        {
            log = new DailyLog { UserId = userId, LogDate = date };
            db.DailyLogs.Add(log);
        }

        log.HasBleeding = request.HasBleeding;
        // Kanama kapalıysa şiddet ve renk anlamsız: formda gizli kaldıkları
        // için eski değerlerin kaydın içinde kalmasına izin vermiyoruz.
        log.Flow = request.HasBleeding ? request.Flow : null;
        log.BloodColor = request.HasBleeding ? request.BloodColor : null;
        log.HasSpotting = request.HasSpotting;
        log.Note = string.IsNullOrWhiteSpace(request.Note)
            ? null
            : request.Note.Trim();
        log.UpdatedAt = DateTimeOffset.UtcNow;

        log.LogSymptoms.Clear();

        foreach (var symptomId in symptomIds)
        {
            log.LogSymptoms.Add(new LogSymptom
            {
                LogId = log.Id,
                SymptomId = symptomId
            });
        }

        // Maske ara tablonun yedeği: risk motoru join yapmadan okuyabilsin.
        log.SymptomMask = SymptomMasks.Of(symptomIds);

        // Kullanıcı her şeyi kaldırdıysa takvimde iz bırakmasın.
        if (log.IsEmpty)
        {
            db.DailyLogs.Remove(log);
            await db.SaveChangesAsync(cancellationToken);

            // Gün silindi: bu döngünün risk özeti artık eski.
            await riskSummaryUpdater.RecomputeForDateAsync(
                userId, date, cancellationToken);

            return Result<DailyLogResponse>.Success(ToResponse(date, null));
        }

        await db.SaveChangesAsync(cancellationToken);

        if (request.HasBleeding)
        {
            var registered = await cycleRegistrar.RegisterBleedingDayAsync(
                user, date, cancellationToken);

            if (registered.IsFailure)
            {
                return Result<DailyLogResponse>.Failure(
                    registered.ErrorCode, registered.Error!);
            }
        }

        // Risk özeti okuma anında değil burada hesaplanır; döngü sınırları
        // yukarıda değişmiş olabileceği için kayıttan sonra çağrılıyor.
        await riskSummaryUpdater.RecomputeForDateAsync(
            userId, date, cancellationToken);

        return Result<DailyLogResponse>.Success(ToResponse(date, log));
    }

    private static DailyLogResponse ToResponse(DateOnly date, DailyLog? log) =>
        new(
            Date: date,
            HasBleeding: log?.HasBleeding ?? false,
            Flow: log?.Flow,
            BloodColor: log?.BloodColor,
            HasSpotting: log?.HasSpotting ?? false,
            SymptomIds: log is null
                ? []
                : [.. log.LogSymptoms.Select(ls => ls.SymptomId).Order()],
            Note: log?.Note);
}
