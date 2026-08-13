namespace SereneCycle.Domain.Entities;

/// <summary>daily_logs ↔ symptoms çoka-çok ara tablosu.</summary>
public class LogSymptom
{
    public Guid LogId { get; set; }
    public DailyLog? Log { get; set; }

    public int SymptomId { get; set; }
    public Symptom? Symptom { get; set; }
}
