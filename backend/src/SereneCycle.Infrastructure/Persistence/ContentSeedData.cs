using SereneCycle.Domain.Content;
using SereneCycle.Domain.Entities;

namespace SereneCycle.Infrastructure.Persistence;

/// <summary>
/// Faz içerik kataloğu — dört fazın listelerinin birleşimi. Katalog
/// çevrimdışı üretilir ve insan denetiminden geçtikten sonra buraya commit
/// edilir: çalışma anında LLM çağrısı yok, yani ağ yok, gecikme yok, ücret
/// yok — deterministik ve test edilebilir.
///
/// <b>Katalog boyutu bir performans ayrıntısı değil, ürünün kendisidir.</b>
/// Ekranda faz başına 3 öneri gösteriliyor; katalogda faz başına 3 öğe
/// varsa hem çeşitlilik hem öğrenme imkânsız hâle gelir — motor her zaman
/// aynı üç öğeyi döndürür, 👍/👎 ve anket hiçbir şeyi değiştiremez.
/// Bu yüzden her faz + tür için gösterilecek sayının kat kat üzerinde aday
/// tutulur.
///
/// Dil bilinçli olarak yumuşak: "yasak" değil "sınırlı tut", ve her maddede
/// gerekçe var. Etiketler iki ayrı sözlükten gelir:
/// <see cref="TasteTag"/> öğrenilen zevk kollarıdır,
/// <see cref="ContraTag"/> ise öğenin "beni şu kullanıcı bayrakları eler"
/// beyanıdır.
/// </summary>
public static class ContentSeedData
{
    public static readonly IReadOnlyList<ContentItem> All =
    [
        .. MenstrualSeedData.All,
        .. FollicularSeedData.All,
        .. OvulationSeedData.All,
        .. LutealSeedData.All
    ];
}
