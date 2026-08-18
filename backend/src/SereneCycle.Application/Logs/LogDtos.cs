using System.ComponentModel.DataAnnotations;
using SereneCycle.Application.Common;
using SereneCycle.Domain.Entities;

namespace SereneCycle.Application.Logs;

/// <summary>Adet kaydı ekranındaki tek bir belirti çipi.</summary>
public sealed record SymptomOption(int Id, string Name);

/// <summary>
/// Bir güne ait kayıt. Kayıt yoksa da bu şekil döner (boş varsayılanlarla)
/// ki mobil taraf "var mı yok mu" ayrımı yapmak zorunda kalmasın.
/// </summary>
public sealed record DailyLogResponse(
    DateOnly Date,
    bool HasBleeding,
    FlowIntensity? Flow,
    BloodColor? BloodColor,
    bool HasSpotting,
    IReadOnlyList<int> SymptomIds,
    string? Note);

/// <summary>
/// Gün kaydını üzerine yazar (upsert). Tüm alanlar gönderilir: ekran
/// formun tamamını tuttuğu için kısmi güncelleme belirsizlik yaratırdı.
/// </summary>
public sealed record SaveDailyLogRequest(
    bool HasBleeding,
    FlowIntensity? Flow,
    BloodColor? BloodColor,
    bool HasSpotting,
    IReadOnlyList<int>? SymptomIds,
    [StringLength(2000)] string? Note);

public interface ILogService
{
    /// <summary>Adet kaydı ekranında gösterilecek belirti listesi.</summary>
    Task<IReadOnlyList<SymptomOption>> GetSymptomOptionsAsync(
        CancellationToken cancellationToken = default);

    Task<Result<DailyLogResponse>> GetAsync(
        Guid userId,
        DateOnly date,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Günü kaydeder. Kanama işaretlendiyse gerekiyorsa yeni bir döngü
    /// başlatılır; kayıt tamamen boşaltıldıysa satır silinir.
    /// </summary>
    Task<Result<DailyLogResponse>> SaveAsync(
        Guid userId,
        DateOnly date,
        SaveDailyLogRequest request,
        CancellationToken cancellationToken = default);
}
