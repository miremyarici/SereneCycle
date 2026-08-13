namespace SereneCycle.Domain.Entities;

/// <summary>Semptom/duygu sözlüğü — seed data ile doldurulur.</summary>
public class Symptom
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public SymptomCategory Category { get; set; }

    public ICollection<LogSymptom> LogSymptoms { get; set; } = [];
}

public enum SymptomCategory
{
    Physical,
    Mood
}
