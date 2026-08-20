using SereneCycle.Domain.Cycles;

namespace SereneCycle.Domain.Content;

/// <summary>
/// Öneri listesinin rastgeleliğini güne sabitleyen tohum. Kullanıcı ekranı
/// yenilediğinde liste zıplamaz, ertesi gün kendiliğinden değişir — bu
/// yüzden sonucu önbelleğe almaya (ve önbellek geçersizleştirmeye) gerek
/// kalmaz.
/// </summary>
public static class DailySeed
{
    // FNV-1a 32 bit. HashCode.Combine bilinçli olarak kullanılmadı: süreç
    // başına rastgele tohumlanır, yani uygulama yeniden başlayınca aynı
    // günün listesi değişirdi.
    private const uint FnvOffsetBasis = 2166136261;
    private const uint FnvPrime = 16777619;

    /// <summary>
    /// Aynı kullanıcı + aynı gün + aynı faz her zaman aynı tohumu verir.
    /// İçerik türü bilinçli olarak dışarıda: "öncelik ver" ve "sınırlı tut"
    /// listeleri aynı isteğin parçası, aynı dünya hipotezini paylaşmalı.
    /// </summary>
    public static int Of(Guid userId, DateOnly date, CyclePhase phase)
    {
        Span<byte> userBytes = stackalloc byte[16];
        userId.TryWriteBytes(userBytes);

        var hash = FnvOffsetBasis;

        foreach (var b in userBytes)
        {
            hash = Mix(hash, b);
        }

        hash = MixInt32(hash, date.DayNumber);
        hash = MixInt32(hash, (int)phase);

        return unchecked((int)hash);
    }

    private static uint MixInt32(uint hash, int value)
    {
        for (var shift = 0; shift < 32; shift += 8)
        {
            hash = Mix(hash, (byte)(value >> shift));
        }

        return hash;
    }

    private static uint Mix(uint hash, byte value) =>
        unchecked((hash ^ value) * FnvPrime);
}
