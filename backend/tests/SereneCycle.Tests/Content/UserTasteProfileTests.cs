using SereneCycle.Domain.Content;
using SereneCycle.Domain.Entities;

namespace SereneCycle.Tests.Content;

public class UserTasteProfileTests
{
    private static UserTasteProfile NewProfile() =>
        UserTasteProfile.CreateFor(Guid.NewGuid());

    /// <summary>Bilgi yokken her etiket Beta(1,1): düzgün dağılım.</summary>
    [Fact]
    public void FreshProfile_StartsUniform()
    {
        var profile = NewProfile();

        Assert.Equal(TasteTags.Capacity, profile.Alpha.Length);
        Assert.Equal(TasteTags.Capacity, profile.Beta.Length);
        Assert.All(profile.Alpha, count => Assert.Equal(1, count));
        Assert.All(profile.Beta, count => Assert.Equal(1, count));
    }

    /// <summary>
    /// Anket prior'ı: <c>α = 1 + s·C</c>, <c>β = 1 + (1 − s)·C</c>, C = 4.
    /// </summary>
    [Fact]
    public void Survey_SetsPriorsWithThePseudoCount()
    {
        var profile = NewProfile();

        profile.ApplySurvey(
            liked: [TasteTag.Yoga],
            disliked: [TasteTag.Hiit]);

        Assert.Equal(5, profile.Alpha[(int)TasteTag.Yoga]);
        Assert.Equal(1, profile.Beta[(int)TasteTag.Yoga]);

        Assert.Equal(1, profile.Alpha[(int)TasteTag.Hiit]);
        Assert.Equal(5, profile.Beta[(int)TasteTag.Hiit]);
    }

    /// <summary>
    /// Cevaplanmamış soru "nötr" değil "bilgi yok" demektir: etiket
    /// Beta(1,1) kalır, yani keşif en yüksek seviyede sürer.
    /// </summary>
    [Fact]
    public void Survey_LeavesUnansweredTagsUniform()
    {
        var profile = NewProfile();

        profile.ApplySurvey(liked: [TasteTag.Yoga], disliked: []);

        Assert.Equal(1, profile.Alpha[(int)TasteTag.Cardio]);
        Assert.Equal(1, profile.Beta[(int)TasteTag.Cardio]);
    }

    /// <summary>
    /// Anket soğuk başlangıç aracıdır, birikimli bir sinyal değil: yeniden
    /// gönderilirse vektör baştan kurulur.
    /// </summary>
    [Fact]
    public void ResubmittedSurvey_ReplacesThePreviousPrior()
    {
        var profile = NewProfile();

        profile.ApplySurvey(liked: [TasteTag.Yoga], disliked: []);
        profile.ApplySurvey(liked: [TasteTag.Cardio], disliked: []);

        Assert.Equal(1, profile.Alpha[(int)TasteTag.Yoga]);
        Assert.Equal(5, profile.Alpha[(int)TasteTag.Cardio]);
    }

    /// <summary>
    /// C = 4 sahte-sayımın anlamı: tek bir geri bildirim anketi henüz
    /// bastıramaz, dört geri bildirim bastırır. Ankette başla, gerçeğe
    /// doğru sön.
    /// </summary>
    [Fact]
    public void FourRealFeedbacks_OverrideTheSurvey()
    {
        var afterOne = NewProfile();
        afterOne.ApplySurvey(liked: [TasteTag.Yoga], disliked: []);
        Dislike(afterOne, TasteTag.Yoga, times: 1);

        // Tek olumsuz sinyal ankete karşı yetmiyor.
        Assert.True(PosteriorMean(afterOne, TasteTag.Yoga) > 0.5);

        var afterFour = NewProfile();
        afterFour.ApplySurvey(liked: [TasteTag.Yoga], disliked: []);
        Dislike(afterFour, TasteTag.Yoga, times: 4);

        Assert.True(PosteriorMean(afterFour, TasteTag.Yoga) < 0.5);
    }

    /// <summary>Geri bildirim yalnızca öğenin kendi etiketlerine yazılır.</summary>
    [Fact]
    public void Feedback_TouchesOnlyTheItemsTags()
    {
        var profile = NewProfile();

        profile.ApplyFeedback(
            TasteTags.Of([TasteTag.Yoga, TasteTag.Stretching]),
            ContentFeedback.Liked);

        Assert.Equal(3, profile.Alpha[(int)TasteTag.Yoga]);
        Assert.Equal(3, profile.Alpha[(int)TasteTag.Stretching]);
        Assert.Equal(1, profile.Alpha[(int)TasteTag.Cardio]);
        Assert.All(profile.Beta, count => Assert.Equal(1, count));
    }

    [Fact]
    public void Dislike_FeedsTheBetaCounter()
    {
        var profile = NewProfile();

        profile.ApplyFeedback(
            TasteTags.Of([TasteTag.Hiit]), ContentFeedback.Disliked);

        Assert.Equal(1, profile.Alpha[(int)TasteTag.Hiit]);
        Assert.Equal(3, profile.Beta[(int)TasteTag.Hiit]);
    }

    /// <summary>"Tamamladım" beğeniden zayıf ama gerçek bir olumlu sinyal.</summary>
    [Fact]
    public void Completed_IsAWeakerPositiveSignalThanALike()
    {
        var completed = NewProfile();
        completed.ApplyFeedback(
            TasteTags.Of([TasteTag.Pilates]), ContentFeedback.Completed);

        var liked = NewProfile();
        liked.ApplyFeedback(
            TasteTags.Of([TasteTag.Pilates]), ContentFeedback.Liked);

        Assert.True(
            completed.Alpha[(int)TasteTag.Pilates]
            < liked.Alpha[(int)TasteTag.Pilates]);
    }

    /// <summary>
    /// Unutma katsayısı sayaçları <c>1 + w / (1 − f)</c> sabit noktasına
    /// kilitler: smallint taşmaz, posterior sonsuz kesinliğe ulaşmaz, yani
    /// keşif hiç bitmez.
    /// </summary>
    [Fact]
    public void Counters_ConvergeInsteadOfGrowingWithoutBound()
    {
        var profile = NewProfile();
        var mask = TasteTags.Of([TasteTag.Cardio]);

        for (var i = 0; i < 1_000; i++)
        {
            profile.ApplyFeedback(mask, ContentFeedback.Liked);
        }

        var converged = profile.Alpha[(int)TasteTag.Cardio];

        for (var i = 0; i < 10_000; i++)
        {
            profile.ApplyFeedback(mask, ContentFeedback.Liked);
        }

        Assert.Equal(converged, profile.Alpha[(int)TasteTag.Cardio]);
        Assert.True(converged < short.MaxValue);
    }

    /// <summary>
    /// Zevk değişimi takip edilir: eski beğeniler yeni olumsuz sinyallerin
    /// altında kalmaz.
    /// </summary>
    [Fact]
    public void ChangedTaste_IsEventuallyFollowed()
    {
        var profile = NewProfile();
        var mask = TasteTags.Of([TasteTag.Sweet]);

        for (var i = 0; i < 100; i++)
        {
            profile.ApplyFeedback(mask, ContentFeedback.Liked);
        }

        Assert.True(PosteriorMean(profile, TasteTag.Sweet) > 0.9);

        for (var i = 0; i < 100; i++)
        {
            profile.ApplyFeedback(mask, ContentFeedback.Disliked);
        }

        Assert.True(PosteriorMean(profile, TasteTag.Sweet) < 0.5);
    }

    /// <summary>θ vektörünün boyu tanımlı etiket sayısı kadardır.</summary>
    [Fact]
    public void SampledScores_CoverEveryDefinedTag()
    {
        var scores = NewProfile().SampleTasteScores(new Random(5));

        Assert.Equal(TasteTags.Count, scores.Length);
        Assert.All(scores, score => Assert.InRange(score, 0, 1));
    }

    [Fact]
    public void SampledScores_AreDeterministicForASeed()
    {
        var profile = NewProfile();

        Assert.Equal(
            profile.SampleTasteScores(new Random(99)),
            profile.SampleTasteScores(new Random(99)));
    }

    private static void Dislike(
        UserTasteProfile profile, TasteTag tag, int times)
    {
        var mask = TasteTags.Of([tag]);

        for (var i = 0; i < times; i++)
        {
            profile.ApplyFeedback(mask, ContentFeedback.Disliked);
        }
    }

    private static double PosteriorMean(UserTasteProfile profile, TasteTag tag)
    {
        double alpha = profile.Alpha[(int)tag];
        double beta = profile.Beta[(int)tag];

        return alpha / (alpha + beta);
    }
}
