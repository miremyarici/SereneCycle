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
        // Fiziksel — adet kaydı ekranındaki belirti listesi
        new() { Id = 1, Name = "Karın krampları", Category = SymptomCategory.Physical },
        new() { Id = 2, Name = "Baş ağrısı", Category = SymptomCategory.Physical },
        new() { Id = 3, Name = "Şişkinlik", Category = SymptomCategory.Physical },
        new() { Id = 4, Name = "Göğüs ağrısı", Category = SymptomCategory.Physical },
        new() { Id = 5, Name = "Yorgunluk", Category = SymptomCategory.Physical },
        new() { Id = 6, Name = "Akne", Category = SymptomCategory.Physical },
        new() { Id = 7, Name = "Bel ağrısı", Category = SymptomCategory.Physical },
        new() { Id = 8, Name = "İştah değişikliği", Category = SymptomCategory.Physical },
        new() { Id = 9, Name = "Ateş basması", Category = SymptomCategory.Physical },
        new() { Id = 10, Name = "Bulantı", Category = SymptomCategory.Physical },
        new() { Id = 11, Name = "Cilt kuruluğu", Category = SymptomCategory.Physical },
        new() { Id = 12, Name = "Gece terlemeleri", Category = SymptomCategory.Physical },
        new() { Id = 13, Name = "Hafıza kaybı", Category = SymptomCategory.Physical },
        new() { Id = 14, Name = "İdrar kaçırma", Category = SymptomCategory.Physical },
        new() { Id = 15, Name = "İshal", Category = SymptomCategory.Physical },
        new() { Id = 16, Name = "Kabızlık", Category = SymptomCategory.Physical },
        new() { Id = 17, Name = "Pelvis ağrısı", Category = SymptomCategory.Physical },
        new() { Id = 18, Name = "Saç dökülmesi", Category = SymptomCategory.Physical },
        new() { Id = 19, Name = "Uyku değişiklikleri", Category = SymptomCategory.Physical },
        new() { Id = 30, Name = "Üşüme", Category = SymptomCategory.Physical },
        new() { Id = 31, Name = "Vajina kuruluğu", Category = SymptomCategory.Physical },

        // Duygu
        new() { Id = 20, Name = "Enerjik", Category = SymptomCategory.Mood },
        new() { Id = 21, Name = "Sakin", Category = SymptomCategory.Mood },
        new() { Id = 22, Name = "Mutlu", Category = SymptomCategory.Mood },
        new() { Id = 23, Name = "Hüzünlü", Category = SymptomCategory.Mood },
        new() { Id = 24, Name = "Kaygılı", Category = SymptomCategory.Mood },
        new() { Id = 25, Name = "Sinirli", Category = SymptomCategory.Mood },
        new() { Id = 26, Name = "Odaklı", Category = SymptomCategory.Mood },
        new() { Id = 27, Name = "Hassas", Category = SymptomCategory.Mood },
        new() { Id = 32, Name = "Duygudurum değişiklikleri", Category = SymptomCategory.Mood }
    ];

    /// <summary>
    /// Adet kaydı ekranında, tasarımdaki sırayla (alfabetik) gösterilen
    /// belirtiler. Sözlükteki her semptom bu ekranda çıkmaz — duygu
    /// çiplerinin çoğu ayrı bir akışa ait — bu yüzden liste burada açıkça
    /// tanımlı; kategoriye göre filtrelemeye bırakılmadı.
    /// </summary>
    public static readonly int[] PeriodLogSymptomIds =
    [
        6,  // Akne
        9,  // Ateş basması
        2,  // Baş ağrısı
        7,  // Bel ağrısı
        10, // Bulantı
        11, // Cilt kuruluğu
        32, // Duygudurum değişiklikleri
        12, // Gece terlemeleri
        4,  // Göğüs ağrısı
        13, // Hafıza kaybı
        14, // İdrar kaçırma
        15, // İshal
        8,  // İştah değişikliği
        16, // Kabızlık
        1,  // Karın krampları
        17, // Pelvis ağrısı
        18, // Saç dökülmesi
        3,  // Şişkinlik
        19, // Uyku değişiklikleri
        30, // Üşüme
        31, // Vajina kuruluğu
        5   // Yorgunluk
    ];
}
