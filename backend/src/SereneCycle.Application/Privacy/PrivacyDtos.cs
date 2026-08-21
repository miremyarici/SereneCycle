using System.ComponentModel.DataAnnotations;
using SereneCycle.Application.Common;
using SereneCycle.Domain.Content;
using SereneCycle.Domain.Entities;

namespace SereneCycle.Application.Privacy;

/// <summary>
/// Hesap silme isteği. Şifre sorulur: telefonu eline geçiren biri hesabı
/// tek dokunuşla yok edememeli — e-posta değişikliğiyle aynı gerekçe.
/// </summary>
public sealed record DeleteAccountRequest(
    [Required] string CurrentPassword);

/// <summary>
/// Dışa aktarımdaki profil bilgisi. Profil fotoğrafı bilinçli olarak dışarıda:
/// 2 MB'lık base64 bir alan JSON'u okunmaz hâle getirir, fotoğrafın kendisi
/// <c>GET /me/avatar</c> ile zaten indirilebiliyor.
/// </summary>
public sealed record ExportedProfile(
    string Name,
    string Email,
    bool EmailConfirmed,
    DateTimeOffset CreatedAt,
    int AvgCycleLength,
    int AvgPeriodLength,
    bool HasAvatar);

public sealed record ExportedCycle(
    DateOnly StartDate,
    DateOnly? EndDate,
    int? PeriodDays,
    int? LengthInDays)
{
    public static ExportedCycle From(Cycle cycle)
    {
        ArgumentNullException.ThrowIfNull(cycle);

        return new ExportedCycle(
            cycle.StartDate,
            cycle.EndDate,
            cycle.PeriodDays,
            cycle.LengthInDays);
    }
}

/// <param name="Symptoms">
/// Semptom kimlikleri değil adları: dosya, dışa aktarıldığı sürümün
/// semptom sözlüğü olmadan da okunabilir olmalı.
/// </param>
public sealed record ExportedDailyLog(
    DateOnly Date,
    bool HasBleeding,
    FlowIntensity? Flow,
    BloodColor? BloodColor,
    bool HasSpotting,
    IReadOnlyList<string> Symptoms,
    string? Note);

/// <param name="Score">
/// Beta posterior'un ortalaması (α / (α + β)): 1'e yakın "bu türden şeyleri
/// seviyor", 0'a yakın "sevmiyor".
/// </param>
public sealed record ExportedTasteScore(TasteTag Tag, double Score);

/// <summary>
/// Öneri motorunun kullanıcı hakkında tuttuğu iki şey: beyan edilen kısıtlar
/// ve öğrenilmiş zevk tahminleri. İkincisi çıkarım yoluyla üretilmiş kişisel
/// veridir; dışa aktarımın asıl anlamlı kısmı da budur.
/// </summary>
public sealed record ExportedPreferences(
    IReadOnlyList<ContraTag> AvoidFlags,
    IReadOnlyList<ExportedTasteScore> LearnedTastes)
{
    /// <summary>
    /// Hiç dokunulmamış etiketler listelenmez: Beta(1,1) "bu kullanıcıyı
    /// %50 seviyor sanıyoruz" değil, "hakkında hiçbir şey bilmiyoruz"
    /// demektir. 24 anlamsız 0.5 satırı dosyayı yanıltıcı yapardı.
    /// </summary>
    public static ExportedPreferences From(
        long avoidMask,
        short[] alpha,
        short[] beta)
    {
        ArgumentNullException.ThrowIfNull(alpha);
        ArgumentNullException.ThrowIfNull(beta);

        var flags = ContraTags.All
            .Where(flag => ContraTags.Contains(avoidMask, flag))
            .ToList();

        var tastes = new List<ExportedTasteScore>();

        foreach (var tag in Enum.GetValues<TasteTag>())
        {
            var index = (int)tag;

            if (index >= alpha.Length || index >= beta.Length)
            {
                continue;
            }

            var (a, b) = (alpha[index], beta[index]);

            if (a == TasteLearning.UniformCount
                && b == TasteLearning.UniformCount)
            {
                continue;
            }

            tastes.Add(new ExportedTasteScore(
                tag, Math.Round((double)a / (a + b), 2)));
        }

        return new ExportedPreferences(flags, tastes);
    }
}

/// <summary>
/// KVKK'nın veri taşınabilirliği hakkının karşılığı: kullanıcının
/// uygulamada ürettiği her şeyin makine tarafından okunabilir tek dosyası.
/// </summary>
/// <param name="Format">
/// Şema sürümü. Dosyayı yıllar sonra okuyan bir aracın hangi düzeni
/// beklediğini bilmesi için sabit bir etiket.
/// </param>
public sealed record UserDataExport(
    string Format,
    DateTimeOffset ExportedAt,
    ExportedProfile Profile,
    IReadOnlyList<ExportedCycle> Cycles,
    IReadOnlyList<ExportedDailyLog> DailyLogs,
    ExportedPreferences Preferences,
    string Notice)
{
    public const string CurrentFormat = "serene-cycle-export-v1";

    public const string DefaultNotice =
        "Bu dosya Serene Cycle hesabındaki bütün kişisel verilerini içerir. "
        + "Menstrüel veri KVKK kapsamında özel nitelikli kişisel veridir; "
        + "dosyayı paylaşırken bunu göz önünde bulundur.";
}

public interface IAccountDataService
{
    /// <summary>Kullanıcının bütün verisini tek bir belgede toplar.</summary>
    Task<Result<UserDataExport>> ExportAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Hesabı ve ona bağlı bütün veriyi kalıcı olarak siler. Geri alınamaz.
    /// </summary>
    Task<Result> DeleteAsync(
        Guid userId,
        DeleteAccountRequest request,
        CancellationToken cancellationToken = default);
}
