using SereneCycle.Application.Privacy;
using SereneCycle.Domain.Content;
using SereneCycle.Domain.Entities;

namespace SereneCycle.Tests.Privacy;

/// <summary>
/// Dışa aktarımın saf kısmı: veritabanından okunan ham sayaçların ve
/// maskelerin kullanıcıya gösterilebilir hâle gelmesi.
/// </summary>
public class UserDataExportTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void AvoidFlags_ListsOnlyFlagsSetInMask()
    {
        var mask = ContraTags.Of([ContraTag.Vegan, ContraTag.Knee]);
        var profile = UserTasteProfile.CreateFor(UserId);

        var exported = ExportedPreferences.From(
            mask, profile.Alpha, profile.Beta);

        Assert.Equal(
            new[] { ContraTag.Vegan, ContraTag.Knee },
            exported.AvoidFlags.Order().ToArray());
    }

    [Fact]
    public void UntouchedTasteTags_AreOmitted()
    {
        // Hiç geri bildirim görmemiş profil: bütün etiketler Beta(1,1).
        // "Hakkında bilgi yok" ile "yarı yarıya seviyor" aynı şey değil.
        var profile = UserTasteProfile.CreateFor(UserId);

        var exported = ExportedPreferences.From(0, profile.Alpha, profile.Beta);

        Assert.Empty(exported.LearnedTastes);
        Assert.Empty(exported.AvoidFlags);
    }

    [Fact]
    public void TouchedTasteTag_IsExportedWithPosteriorMean()
    {
        var profile = UserTasteProfile.CreateFor(UserId);
        profile.ApplySurvey(liked: [TasteTag.Yoga], disliked: [TasteTag.Hiit]);

        var exported = ExportedPreferences.From(0, profile.Alpha, profile.Beta);

        // Anket prior'ı: sevilen etiket Beta(5,1), sevilmeyen Beta(1,5).
        var yoga = Assert.Single(
            exported.LearnedTastes, t => t.Tag == TasteTag.Yoga);
        var hiit = Assert.Single(
            exported.LearnedTastes, t => t.Tag == TasteTag.Hiit);

        Assert.Equal(0.83, yoga.Score);
        Assert.Equal(0.17, hiit.Score);

        // Ankette geçmeyen etiketler yine listelenmemeli.
        Assert.Equal(2, exported.LearnedTastes.Count);
    }

    [Fact]
    public void OpenCycle_ExportsNullLength()
    {
        var cycle = new Cycle
        {
            UserId = UserId,
            StartDate = new DateOnly(2026, 8, 1),
            PeriodDays = 5
        };

        var exported = ExportedCycle.From(cycle);

        Assert.Null(exported.EndDate);
        Assert.Null(exported.LengthInDays);
        Assert.Equal(5, exported.PeriodDays);
    }

    [Fact]
    public void ClosedCycle_ExportsComputedLength()
    {
        var cycle = new Cycle
        {
            UserId = UserId,
            StartDate = new DateOnly(2026, 8, 1),
            EndDate = new DateOnly(2026, 8, 29)
        };

        var exported = ExportedCycle.From(cycle);

        Assert.Equal(28, exported.LengthInDays);
    }
}
