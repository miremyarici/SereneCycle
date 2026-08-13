using SereneCycle.Domain.Entities;

namespace SereneCycle.Infrastructure.Persistence;

/// <summary>
/// Günlük kayıt ekranındaki semptom/duygu çipleri. Id'ler sabit tutulur ki
/// migration'lar arasında değişmesin ve mobil taraftaki drift seed'iyle
/// aynı kalabilsin.
/// </summary>
public static class SymptomSeedData
{
    public static readonly Symptom[] All =
    [
        // Fiziksel
        new() { Id = 1, Name = "Kramp", Category = SymptomCategory.Physical },
        new() { Id = 2, Name = "Baş ağrısı", Category = SymptomCategory.Physical },
        new() { Id = 3, Name = "Şişkinlik", Category = SymptomCategory.Physical },
        new() { Id = 4, Name = "Göğüs hassasiyeti", Category = SymptomCategory.Physical },
        new() { Id = 5, Name = "Yorgunluk", Category = SymptomCategory.Physical },
        new() { Id = 6, Name = "Sivilce", Category = SymptomCategory.Physical },
        new() { Id = 7, Name = "Bel ağrısı", Category = SymptomCategory.Physical },
        new() { Id = 8, Name = "İştah değişimi", Category = SymptomCategory.Physical },

        // Duygu
        new() { Id = 20, Name = "Enerjik", Category = SymptomCategory.Mood },
        new() { Id = 21, Name = "Sakin", Category = SymptomCategory.Mood },
        new() { Id = 22, Name = "Mutlu", Category = SymptomCategory.Mood },
        new() { Id = 23, Name = "Hüzünlü", Category = SymptomCategory.Mood },
        new() { Id = 24, Name = "Kaygılı", Category = SymptomCategory.Mood },
        new() { Id = 25, Name = "Sinirli", Category = SymptomCategory.Mood },
        new() { Id = 26, Name = "Odaklı", Category = SymptomCategory.Mood },
        new() { Id = 27, Name = "Hassas", Category = SymptomCategory.Mood }
    ];
}
