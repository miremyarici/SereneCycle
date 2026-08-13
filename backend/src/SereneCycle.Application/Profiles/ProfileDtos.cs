using System.ComponentModel.DataAnnotations;
using SereneCycle.Application.Auth;
using SereneCycle.Application.Common;

namespace SereneCycle.Application.Profiles;

/// <summary>
/// Profil + döngü ayarları güncellemesi. Onboarding sihirbazı da bunu kullanır:
/// <see cref="LastPeriodStart"/> verilirse ilk döngü kaydı açılır.
/// </summary>
public sealed record UpdateProfileRequest(
    [StringLength(60, MinimumLength = 1)] string? Name,
    [Range(21, 45)] int? AvgCycleLength,
    [Range(1, 14)] int? AvgPeriodLength,
    DateOnly? LastPeriodStart);

public interface IProfileService
{
    Task<Result<UserSummary>> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Result<UserSummary>> UpdateAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default);
}
