using SereneCycle.Domain.Entities;
using SereneCycle.Domain.Risk;

namespace SereneCycle.Application.Risk;

/// <summary>
/// Ana sayfadaki risk kartı. Tek bir özet satırından türetilir; kartın
/// gösterdiği her sayı o satırda hazır durur.
/// </summary>
/// <param name="Level">Kartın rengini/tonunu belirleyen genel seviye.</param>
/// <param name="Title">Kart başlığı — seviyeye göre değişir.</param>
/// <param name="Message">Başlığın altındaki tek cümlelik özet.</param>
/// <param name="Flags">
/// Dikkat çeken işaretler, ağırdan hafife sıralı. Boşsa kart "şimdilik bir
/// şey yok" durumundadır.
/// </param>
public sealed record RiskCard(
    RiskLevel Level,
    string Title,
    string Message,
    IReadOnlyList<RiskFlagView> Flags,
    RiskCardStats Stats,
    string Disclaimer);

/// <summary>Kartta bir satır olarak gösterilen tek işaret.</summary>
public sealed record RiskFlagView(
    RiskFlagCode Code,
    RiskLevel Level,
    string Title,
    string Detail);

/// <summary>
/// Kartın alt şeridindeki nötr sayılar. İşaret üretmeseler de kullanıcının
/// döngüsünü özetledikleri için gösteriliyorlar.
/// </summary>
public sealed record RiskCardStats(
    int LoggedDays,
    int BleedingDays,
    int SpottingDays,
    int PainDays);

public interface IRiskService
{
    /// <summary>
    /// Verilen döngünün risk kartını döner. Özet satırı yoksa (henüz hiç
    /// hesaplanmamış eski bir döngü) bir kez hesaplanıp saklanır, sonraki
    /// okumalar tek satır getirisine düşer.
    /// </summary>
    Task<RiskCard> GetCardAsync(
        Cycle cycle,
        DateOnly today,
        CancellationToken cancellationToken = default);
}
