namespace SereneCycle.Domain.Cycles;

/// <summary>
/// <see cref="PhaseCalculator.Calculate"/> sonucu: kullanıcının bugün hangi
/// fazda olduğu ve bir sonraki adet/ovulasyon tahminleri.
/// </summary>
/// <param name="CurrentPhase">Bugünün döngü fazı.</param>
/// <param name="CycleDay">Döngü içindeki gün (1'den başlar).</param>
/// <param name="CycleLength">Hesaplamada kullanılan ortalama döngü uzunluğu.</param>
/// <param name="PredictedNextPeriod">Tahmini bir sonraki adet başlangıcı (S + C).</param>
/// <param name="PredictedOvulation">Tahmini ovulasyon günü (S + C - 14).</param>
/// <param name="IsIrregular">
/// Döngü uzunluğu 21-35 gün aralığının dışındaysa true; bu durumda tahminlerin
/// güveni düşüktür ve kullanıcıya uyarı gösterilmelidir.
/// </param>
/// <param name="IsPeriodLate">
/// Bugün beklenen adet gününü geçtiyse true. Faz yine luteal kabul edilir,
/// kullanıcı yeni adet başlangıcını girene kadar tahmin kaymış sayılır.
/// </param>
public sealed record PhasePrediction(
    CyclePhase CurrentPhase,
    int CycleDay,
    int CycleLength,
    DateOnly PredictedNextPeriod,
    DateOnly PredictedOvulation,
    bool IsIrregular,
    bool IsPeriodLate);
