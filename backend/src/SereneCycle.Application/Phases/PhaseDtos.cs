using SereneCycle.Application.Common;
using SereneCycle.Application.Risk;
using SereneCycle.Domain.Cycles;
using SereneCycle.Domain.Entities;

namespace SereneCycle.Application.Phases;

/// <summary>
/// Ana sayfanın tek çağrıda ihtiyaç duyduğu her şey: faz kartı, ilerleme
/// halkası, yatay takvim şeridi, "bu fazda yaygın duygular" ve risk kartı.
/// </summary>
/// <param name="Risk">
/// Risk kartı. Ayrı bir uç nokta yerine burada: ana sayfa zaten bu çağrıyı
/// yapıyor ve özet okuması tek satır getirisi — ikinci bir ağ gidiş-dönüşü
/// mikrosaniyelik hesaptan çok daha pahalı olurdu.
/// </param>
public sealed record PhaseTodayResponse(
    CyclePhase Phase,
    string PhaseName,
    string PhaseDescription,
    int CycleDay,
    int CycleLength,
    DateOnly CycleStartDate,
    DateOnly PredictedNextPeriod,
    DateOnly PredictedOvulation,
    bool IsIrregular,
    bool IsPeriodLate,
    IReadOnlyList<string> CommonMoods,
    IReadOnlyList<CalendarDay> CalendarStrip,
    RiskCard Risk);

/// <summary>Takvim şeridindeki tek bir gün.</summary>
/// <param name="IsPeriodDay">
/// Tahmine dayalı adet günü. Kullanıcının gerçekten kanama işaretlediği gün
/// <see cref="HasBleeding"/> ile ayrı taşınır: tahmin ile kayıt karışmasın.
/// </param>
/// <param name="BloodColor">
/// Kullanıcının o güne girdiği kan rengi; takvimdeki damla bu renge göre
/// boyanır. Kanama işaretli olup renk seçilmediyse null.
/// </param>
public sealed record CalendarDay(
    DateOnly Date,
    int CycleDay,
    CyclePhase Phase,
    bool IsToday,
    bool IsPeriodDay,
    bool HasLog,
    bool HasBleeding,
    BloodColor? BloodColor,
    bool HasSpotting);

/// <summary>Aylık takvim ekranının verisi.</summary>
public sealed record CalendarMonthResponse(
    int Year,
    int Month,
    IReadOnlyList<CalendarDay> Days);

public interface IPhaseService
{
    /// <summary>
    /// Kullanıcının bugünkü faz bilgisini döner. Hiç döngü kaydı yoksa
    /// (onboarding tamamlanmamışsa) <see cref="ErrorCode.NotFound"/> döner.
    /// </summary>
    Task<Result<PhaseTodayResponse>> GetTodayAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verilen ayın tüm günleri. Her gün için o tarihte geçerli olan döngüye
    /// göre faz, ve kullanıcının kaydından gelen kanama/lekelenme işaretleri.
    /// </summary>
    Task<Result<CalendarMonthResponse>> GetMonthAsync(
        Guid userId,
        int year,
        int month,
        CancellationToken cancellationToken = default);
}
