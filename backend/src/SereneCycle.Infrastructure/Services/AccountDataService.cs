using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SereneCycle.Application.Common;
using SereneCycle.Application.Privacy;
using SereneCycle.Domain.Entities;
using SereneCycle.Infrastructure.Identity;
using SereneCycle.Infrastructure.Persistence;

namespace SereneCycle.Infrastructure.Services;

/// <summary>
/// KVKK'nın iki hakkının karşılığı: veri taşınabilirliği (dışa aktarma) ve
/// unutulma (hesap silme). İkisi de profil düzenlemekten farklı bir şey
/// yapıyor — biri kullanıcının bütün verisini okuyor, diğeri hepsini
/// siliyor — bu yüzden <see cref="ProfileService"/>'in içinde değil.
/// </summary>
public class AccountDataService(
    UserManager<AppUser> userManager,
    AppDbContext db) : IAccountDataService
{
    public async Task<Result<UserDataExport>> ExportAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var profile = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                Export = new ExportedProfile(
                    u.Name,
                    u.Email!,
                    u.EmailConfirmed,
                    u.CreatedAt,
                    u.AvgCycleLength,
                    u.AvgPeriodLength,
                    u.AvatarUpdatedAt != null),
                u.AvoidMask
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (profile is null)
        {
            return Result<UserDataExport>.Failure(
                ErrorCode.NotFound, ServiceErrors.UserNotFound);
        }

        var cycles = await db.Cycles
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.StartDate)
            .ToListAsync(cancellationToken);

        // Semptomlar kimlikle değil adla dışa aktarılıyor; ad yalnızca
        // ara tablodan gelebildiği için projeksiyon içinde toplanıyor.
        var logs = await db.DailyLogs
            .AsNoTracking()
            .Where(l => l.UserId == userId)
            .OrderBy(l => l.LogDate)
            .Select(l => new ExportedDailyLog(
                l.LogDate,
                l.HasBleeding,
                l.Flow,
                l.BloodColor,
                l.HasSpotting,
                l.LogSymptoms
                    .Select(ls => ls.Symptom!.Name)
                    .OrderBy(name => name)
                    .ToList(),
                l.Note))
            .ToListAsync(cancellationToken);

        var taste = await db.UserTasteProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken)
            ?? UserTasteProfile.CreateFor(userId);

        return Result<UserDataExport>.Success(new UserDataExport(
            UserDataExport.CurrentFormat,
            DateTimeOffset.UtcNow,
            profile.Export,
            cycles.Select(ExportedCycle.From).ToList(),
            logs,
            ExportedPreferences.From(
                profile.AvoidMask, taste.Alpha, taste.Beta),
            UserDataExport.DefaultNotice));
    }

    public async Task<Result> DeleteAsync(
        Guid userId,
        DeleteAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return Result.Failure(ErrorCode.NotFound, ServiceErrors.UserNotFound);
        }

        // Geri alınamaz bir işlem için token yetmez: cihazı eline geçiren
        // biri hesabı silememeli.
        if (!await userManager.CheckPasswordAsync(user, request.CurrentPassword))
        {
            return Result.Failure(ErrorCode.Unauthorized, "Şifre hatalı.");
        }

        await using var transaction =
            await db.Database.BeginTransactionAsync(cancellationToken);

        // Döngü ve gün kayıtları kullanıcıya yabancı anahtarla bağlı değil —
        // UserId düz bir kolon — bu yüzden kullanıcı satırını silmek onları
        // düşürmez. Yetim kalmasınlar diye açıkça siliniyorlar; risk
        // özetleri de döngü kaskadına güvenmek yerine burada.
        // Sıra yabancı anahtarları takip ediyor: özet → döngü.
        await db.CycleRiskSummaries
            .Where(s => s.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        await db.DailyLogs
            .Where(l => l.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        await db.Cycles
            .Where(c => c.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        // Kullanıcı satırına kaskadla bağlı olanlar (refresh token'lar,
        // zevk profili, Identity talepleri) veritabanı tarafından silinir.
        var deleted = await userManager.DeleteAsync(user);

        if (!deleted.Succeeded)
        {
            return Result.Failure(
                ErrorCode.Validation,
                string.Join(" ", deleted.Errors.Select(e => e.Description)));
        }

        await transaction.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
