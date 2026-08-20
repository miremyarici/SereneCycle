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

        // İki kolon yeter: varlığın tamamını çekip izlemeye almanın anlamı
        // yok, sözlük zaten salt okunur bir listeye dönüşecek.
        var names = await db.Symptoms
            .AsNoTracking()
            .Where(s => ids.Contains(s.Id))
            .Select(s => new { s.Id, s.Name })
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        // Sıra seed listesinden gelir; veritabanı sırasına güvenmiyoruz.
        var options = new List<SymptomOption>(ids.Length);

        foreach (var id in ids)
        {
            // Tek arama: önceki hâl ContainsKey + indeksleyici ile aynı
            // anahtarı iki kez tarıyordu.
            if (names.TryGetValue(id, out var name))
            {
                options.Add(new SymptomOption(id, name));
            }
        }

        return options;
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
        // Varlık kontrolü ucuz bir sorguyla yapılır; kullanıcı satırının
        // tamamı (satır içinde tutulan 2 MB'a kadar avatar dahil) yalnızca
        // döngü kaydı gerçekten gerektiğinde çekilir.
        var userExists = await db.Users
            .AnyAsync(u => u.Id == userId, cancellationToken);

        if (!userExists)
        {
            return Result<DailyLogResponse>.Failure(
                ErrorCode.NotFound, ServiceErrors.UserNotFound);
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
            // Döngü kaydı ortalama döngü uzunluğunu güncelleyebildiği için
            // burada izlenen varlığın kendisi gerekiyor.
            var user = await userManager.FindByIdAsync(userId.ToString());

            if (user is null)
            {
                return Result<DailyLogResponse>.Failure(
                    ErrorCode.NotFound, ServiceErrors.UserNotFound);
            }

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
