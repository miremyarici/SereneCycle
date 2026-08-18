namespace SereneCycle.Domain.Entities;

/// <summary>Bir güne ait kayıt: kanama, akıntı şiddeti, kan rengi,
/// lekelenme, semptom seçimleri ve not.</summary>
public class DailyLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public DateOnly LogDate { get; set; }

    /// <summary>
    /// Kullanıcının "kanama var" anahtarı. <see cref="Flow"/> ve
    /// <see cref="BloodColor"/> yalnızca bu true iken anlamlıdır.
    /// </summary>
    public bool HasBleeding { get; set; }

    /// <summary>0 yok, 1 az, 2 orta, 3 yoğun.</summary>
    public FlowIntensity? Flow { get; set; }

    public BloodColor? BloodColor { get; set; }

    /// <summary>Adet dışı hafif lekelenme; kanamadan bağımsız işaretlenir.</summary>
    public bool HasSpotting { get; set; }

    public string? Note { get; set; }

    /// <summary>
    /// <see cref="LogSymptoms"/> içindeki id'lerin bit maskesi — aynı verinin
    /// yedeği. Risk motoru bir döngünün bütün günlerini tararken semptom
    /// join'i yapmak zorunda kalmasın diye var: satır başına 8 bayta karşılık
    /// kartezyen satır patlaması riski tamamen kalkıyor.
    /// Tek yazma yeri <c>LogService</c>.
    /// </summary>
    public long SymptomMask { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<LogSymptom> LogSymptoms { get; set; } = [];

    /// <summary>
    /// Kullanıcı her şeyi temizlediyse kaydı saklamanın anlamı yok;
    /// takvimde "kayıt var" işareti bırakmaması için silinir.
    /// </summary>
    public bool IsEmpty =>
        !HasBleeding
        && !HasSpotting
        && Flow is null
        && BloodColor is null
        && string.IsNullOrWhiteSpace(Note)
        && LogSymptoms.Count == 0;
}

public enum FlowIntensity
{
    None = 0,
    Light = 1,
    Medium = 2,
    Heavy = 3
}

/// <summary>Adet kaydında seçilebilen kan rengi.</summary>
public enum BloodColor
{
    Red = 0,
    Brown = 1,
    Pink = 2,
    Black = 3,
    Orange = 4,
    Gray = 5
}
