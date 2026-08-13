namespace SereneCycle.Domain.Entities;

/// <summary>Bir güne ait kayıt: akıntı şiddeti, semptom/duygu seçimleri, not.</summary>
public class DailyLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public DateOnly LogDate { get; set; }

    /// <summary>0 yok, 1 az, 2 orta, 3 yoğun.</summary>
    public FlowIntensity? Flow { get; set; }

    public string? Note { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<LogSymptom> LogSymptoms { get; set; } = [];
}

public enum FlowIntensity
{
    None = 0,
    Light = 1,
    Medium = 2,
    Heavy = 3
}
