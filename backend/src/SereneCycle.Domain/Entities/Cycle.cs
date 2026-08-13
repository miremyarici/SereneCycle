namespace SereneCycle.Domain.Entities;

/// <summary>
/// Bir adet başlangıcıyla açılan döngü. Yeni adet başlayınca
/// <see cref="EndDate"/> dolar ve yeni bir kayıt açılır.
/// </summary>
public class Cycle
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    /// <summary>Adetin ilk günü.</summary>
    public DateOnly StartDate { get; set; }

    /// <summary>Bir sonraki döngü başlayana kadar null.</summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>Kanamanın sürdüğü gün sayısı; kullanıcı girmediyse null.</summary>
    public int? PeriodDays { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Tamamlanmış döngünün gün cinsinden uzunluğu; henüz kapanmadıysa null.
    /// </summary>
    public int? LengthInDays =>
        EndDate is null ? null : EndDate.Value.DayNumber - StartDate.DayNumber;
}
