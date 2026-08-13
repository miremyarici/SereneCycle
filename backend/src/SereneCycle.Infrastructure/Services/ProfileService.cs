using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SereneCycle.Application.Auth;
using SereneCycle.Application.Common;
using SereneCycle.Application.Profiles;
using SereneCycle.Domain.Cycles;
using SereneCycle.Domain.Entities;
using SereneCycle.Infrastructure.Identity;
using SereneCycle.Infrastructure.Persistence;

namespace SereneCycle.Infrastructure.Services;

public class ProfileService(
    UserManager<AppUser> userManager,
    AppDbContext db) : IProfileService
{
    public async Task<Result<UserSummary>> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return Result<UserSummary>.Failure(
                ErrorCode.NotFound, "Kullanıcı bulunamadı.");
        }

        return Result<UserSummary>.Success(
            await ToSummaryAsync(user, cancellationToken));
    }

    public async Task<Result<UserSummary>> UpdateAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return Result<UserSummary>.Failure(
                ErrorCode.NotFound, "Kullanıcı bulunamadı.");
        }

        if (request.Name is not null)
        {
            user.Name = request.Name.Trim();
        }

        if (request.AvgCycleLength is not null)
        {
            user.AvgCycleLength = request.AvgCycleLength.Value;
        }

        if (request.AvgPeriodLength is not null)
        {
            user.AvgPeriodLength = request.AvgPeriodLength.Value;
        }

        var updated = await userManager.UpdateAsync(user);

        if (!updated.Succeeded)
        {
            return Result<UserSummary>.Failure(
                ErrorCode.Validation,
                string.Join(" ", updated.Errors.Select(e => e.Description)));
        }

        // Onboarding sihirbazı son adet tarihini de gönderir: ilk döngüyü açar.
        if (request.LastPeriodStart is { } startDate)
        {
            var result = await StartCycleAsync(
                user, startDate, cancellationToken);

            if (result.IsFailure)
            {
                return Result<UserSummary>.Failure(
                    result.ErrorCode, result.Error!);
            }
        }

        return Result<UserSummary>.Success(
            await ToSummaryAsync(user, cancellationToken));
    }

    /// <summary>
    /// Yeni adet başlangıcı kaydeder: açık döngüyü kapatır, yenisini açar ve
    /// ortalama döngü uzunluğunu yeniden hesaplar.
    /// </summary>
    private async Task<Result> StartCycleAsync(
        AppUser user,
        DateOnly startDate,
        CancellationToken cancellationToken)
    {
        if (startDate > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return Result.Failure(
                ErrorCode.Validation,
                "Adet başlangıcı gelecek bir tarih olamaz.");
        }

        var alreadyExists = await db.Cycles.AnyAsync(
            c => c.UserId == user.Id && c.StartDate == startDate,
            cancellationToken);

        if (alreadyExists)
        {
            return Result.Success();
        }

        var previous = await db.Cycles
            .Where(c => c.UserId == user.Id && c.StartDate < startDate)
            .OrderByDescending(c => c.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (previous is not null)
        {
            previous.EndDate = startDate;
            previous.UpdatedAt = DateTimeOffset.UtcNow;
        }

        db.Cycles.Add(new Cycle
        {
            UserId = user.Id,
            StartDate = startDate
        });

        await db.SaveChangesAsync(cancellationToken);
        await RecomputeAverageCycleLengthAsync(user, cancellationToken);

        return Result.Success();
    }

    private async Task RecomputeAverageCycleLengthAsync(
        AppUser user,
        CancellationToken cancellationToken)
    {
        var completed = await db.Cycles
            .Where(c => c.UserId == user.Id && c.EndDate != null)
            .OrderByDescending(c => c.StartDate)
            .Take(CycleStats.DefaultWindow)
            .Select(c => c.EndDate!.Value.DayNumber - c.StartDate.DayNumber)
            .ToListAsync(cancellationToken);

        var recomputed = CycleStats.RecomputeAverageCycleLength(
            completed, fallback: user.AvgCycleLength);

        if (recomputed == user.AvgCycleLength)
        {
            return;
        }

        user.AvgCycleLength = recomputed;
        await userManager.UpdateAsync(user);
    }

    private async Task<UserSummary> ToSummaryAsync(
        AppUser user,
        CancellationToken cancellationToken)
    {
        var hasCycle = await db.Cycles
            .AnyAsync(c => c.UserId == user.Id, cancellationToken);

        return new UserSummary(
            user.Id,
            user.Name,
            user.Email!,
            user.EmailConfirmed,
            user.AvgCycleLength,
            user.AvgPeriodLength,
            HasCompletedOnboarding: hasCycle);
    }
}
