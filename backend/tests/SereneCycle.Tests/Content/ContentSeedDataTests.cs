using SereneCycle.Domain.Content;
using SereneCycle.Domain.Cycles;
using SereneCycle.Domain.Entities;
using SereneCycle.Infrastructure.Persistence;

namespace SereneCycle.Tests.Content;

/// <summary>
/// Kataloğun kendisinin testleri. Öneri motoru bir sıralama fonksiyonudur:
/// gösterilecek sayı kadar aday varsa hiçbir sıralama fark yaratmaz —
/// kullanıcı her açılışta aynı üç öğeyi görür, 👍/👎 ve anket hiçbir şeyi
/// değiştiremez. Yani "katalog yeterince geniş mi" bir veri girişi ayrıntısı
/// değil, motorun çalışma koşuludur; burada sınanır.
/// </summary>
public class ContentSeedDataTests
{
    /// <summary><c>ContentService.RecommendedCount</c> ile aynı sayı.</summary>
    private const int ShownRecommendedCount = 3;

    /// <summary><c>ContentService.LimitedCount</c> ile aynı sayı.</summary>
    private const int ShownLimitedCount = 2;

    /// <summary>
    /// Aday sayısı gösterilenin en az bu katı olmalı. Dört seçildi: bir 👎
    /// öğeyi listeden düşürdüğünde yerine geçecek, ertesi gün günlük tohum
    /// değiştiğinde de başkası çıkacak kadar fazlalık kalsın.
    /// </summary>
    private const int MinimumCandidateMultiple = 4;

    /// <summary>Mobildeki süre çipleriyle aynı seçenekler.</summary>
    public static TheoryData<int> DurationFilterOptions => [10, 15, 30, 45];

    public static TheoryData<CyclePhase> AllPhases =>
        [.. Enum.GetValues<CyclePhase>()];

    /// <summary>
    /// Veritabanı <c>Id</c>'leri satır ekleme sırasına göre üretir; seed
    /// nesnelerinde ise <c>Id</c> hep 0. Testler sıralama ve tekrar
    /// engelleme kurallarını gerçek koşulda sınayabilsin diye katalog
    /// kopyalanıp 1'den numaralanır — paylaşılan statik liste değiştirilmez.
    /// </summary>
    private static readonly IReadOnlyList<ContentItem> Catalog =
    [
        .. ContentSeedData.All.Select((item, index) => new ContentItem
        {
            Id = index + 1,
            Phase = item.Phase,
            Type = item.Type,
            Title = item.Title,
            Body = item.Body,
            TagMask = item.TagMask,
            ContraMask = item.ContraMask,
            DurationMinutes = item.DurationMinutes
        })
    ];

    /// <summary>
    /// Kullanıcı 14 gün boyunca aynı fazda kalıyor; her faz + tür için
    /// gösterilenin kat kat üzerinde aday olmak zorunda.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllPhases))]
    public void EveryPhaseAndType_HasFarMoreCandidatesThanShown(CyclePhase phase)
    {
        foreach (var (type, shown) in ShownCounts)
        {
            var candidates = CandidatesOf(phase, type).Count;

            Assert.True(
                candidates >= shown * MinimumCandidateMultiple,
                $"{phase}/{type}: {shown} öğe gösteriliyor ama katalogda "
                + $"yalnızca {candidates} aday var.");
        }
    }

    /// <summary>
    /// Seeder satırları (faz, tür, başlık) üçlüsüyle eşler ve bu üçlüyü
    /// sözlük anahtarı yapar: kopya bir anahtar ikinci çalıştırmada
    /// doğrudan çökme demek.
    /// </summary>
    [Fact]
    public void ContentKeys_AreUnique()
    {
        var duplicates = ContentSeedData.All
            .GroupBy(item => (item.Phase, item.Type, item.Title))
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        Assert.Empty(duplicates);
    }

    /// <summary>
    /// Etiketsiz öneri her zaman nötr skorlanır: ne ankete ne geri
    /// bildirime tepki verir, yani öğrenmeye görünmez.
    /// </summary>
    [Fact]
    public void Recommendations_AlwaysCarryTasteTags()
    {
        var untagged = ContentSeedData.All
            .Where(item => item.Type is ContentType.FoodDo
                or ContentType.ExerciseDo)
            .Where(item => item.TagMask == 0)
            .Select(item => item.Title)
            .ToList();

        Assert.Empty(untagged);
    }

    /// <summary>
    /// Süre yalnızca egzersizde dolu: yiyeceğe süre yazmak onu süre
    /// filtresine sokar, uyarıya süre yazmak ise uyarıyı "10 dakikam var"
    /// diyen kullanıcıdan gizler.
    /// </summary>
    [Fact]
    public void OnlyExercises_DeclareDuration()
    {
        Assert.All(ContentSeedData.All, item =>
        {
            if (item.Type == ContentType.ExerciseDo)
            {
                Assert.NotNull(item.DurationMinutes);
            }
            else
            {
                Assert.Null(item.DurationMinutes);
            }
        });
    }

    /// <summary>
    /// Katalog büyürken en kolay kaçırılan hata: hayvansal bir öğeye diyet
    /// bayrağını işlemeyi unutmak. Vejetaryen kullanıcıya et önermek
    /// istatistiksel bir kayma değil, doğrudan bir hata olur.
    /// </summary>
    [Fact]
    public void AnimalProducts_DeclareTheDietFlagsThatMustFilterThem()
    {
        // Yumurta ve süt vejetaryen beslenmeye dahildir, bu yüzden yalnızca
        // vegan bayrağını (ve kendi alerjilerini) taşırlar.
        (TasteTag Taste, ContraTag[] Required)[] rules =
        [
            (TasteTag.RedMeat, [ContraTag.Vegetarian, ContraTag.Vegan]),
            (TasteTag.Poultry, [ContraTag.Vegetarian, ContraTag.Vegan]),
            (TasteTag.Fish, [ContraTag.Vegetarian, ContraTag.Vegan]),
            (TasteTag.Eggs, [ContraTag.Vegan, ContraTag.EggAllergy]),
            (TasteTag.Dairy, [ContraTag.Vegan, ContraTag.Lactose])
        ];

        foreach (var item in ContentSeedData.All.Where(IsRecommendation))
        {
            foreach (var (taste, required) in rules)
            {
                if (!TasteTags.Contains(item.TagMask, taste))
                {
                    continue;
                }

                foreach (var flag in required)
                {
                    Assert.True(
                        ContraTags.Contains(item.ContraMask, flag),
                        $"'{item.Title}' {taste} etiketli ama {flag} "
                        + "kısıtını beyan etmiyor.");
                }
            }
        }
    }

    /// <summary>
    /// Uyarılara kısıt işlenmez: bir uyarının hamile ya da sakatlıklı
    /// kullanıcıdan gizlenmesi tam ters etkiyi yaratırdı.
    /// </summary>
    [Fact]
    public void Warnings_NeverDeclareConstraints()
    {
        var warningsWithConstraints = ContentSeedData.All
            .Where(item => !IsRecommendation(item) && item.ContraMask != 0)
            .Select(item => item.Title)
            .ToList();

        Assert.Empty(warningsWithConstraints);
    }

    /// <summary>
    /// Kullanıcının şikâyet ettiği durum: süreyi 15 dakikaya çekince liste
    /// boş kalıyordu. Test en kötü hâli alır — bütün kısıt bayrakları set
    /// edilmiş kullanıcı — ve her fazda, her süre seçeneğinde listenin
    /// <b>tam</b> dolduğunu ister.
    /// </summary>
    [Theory]
    [MemberData(nameof(DurationFilterOptions))]
    public void WithEveryConstraintFlag_EveryDurationFilterStillFillsTheList(
        int availableMinutes)
    {
        var everyFlag = ContraTags.Of(ContraTags.All);

        foreach (var phase in Enum.GetValues<CyclePhase>())
        {
            var selected = Recommender.Select(
                CandidatesOf(phase, ContentType.ExerciseDo),
                new RecommendationContext
                {
                    AvoidMask = everyFlag,
                    AvailableMinutes = availableMinutes,
                    Count = ShownRecommendedCount
                },
                ContentTestData.NeutralScores());

            Assert.Equal(ShownRecommendedCount, selected.Count);
        }
    }

    /// <summary>
    /// Beslenme tarafının aynı garantisi: vegan, glutensiz, laktozsuz ve
    /// kuruyemiş alerjisi olan kullanıcı da her fazda tam liste görür.
    /// </summary>
    [Fact]
    public void RestrictedUser_StillGetsAFullNutritionList()
    {
        var avoidMask = ContraTags.Of([
            ContraTag.Vegan,
            ContraTag.Vegetarian,
            ContraTag.Gluten,
            ContraTag.Lactose,
            ContraTag.TreeNuts,
            ContraTag.Shellfish,
            ContraTag.EggAllergy
        ]);

        foreach (var phase in Enum.GetValues<CyclePhase>())
        {
            var selected = Recommender.Select(
                CandidatesOf(phase, ContentType.FoodDo),
                new RecommendationContext
                {
                    AvoidMask = avoidMask,
                    Count = ShownRecommendedCount
                },
                ContentTestData.NeutralScores());

            Assert.Equal(ShownRecommendedCount, selected.Count);
        }
    }

    /// <summary>
    /// Kullanıcının ikinci şikâyeti: 👎 hiçbir şeyi değiştirmiyordu. Katalog
    /// gösterilen sayı kadar olduğunda bu bir hata değil matematiksel
    /// zorunluluktu; artık beğenilmeyen öğe listeden düşmek zorunda.
    ///
    /// Test θ örneklemesi yerine posterior ortalamasını kullanır: rastgelelik
    /// olmadan da geri bildirimin skoru düşürdüğü görülsün.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllPhases))]
    public void Dislike_DropsTheItemFromTheRecommendedList(CyclePhase phase)
    {
        var candidates = CandidatesOf(phase, ContentType.FoodDo);
        var context = new RecommendationContext { Count = ShownRecommendedCount };
        var profile = UserTasteProfile.CreateFor(Guid.NewGuid());

        var before = Recommender.Select(
            candidates, context, PosteriorMeans(profile));

        var rejected = before[0];
        profile.ApplyFeedback(rejected.TagMask, ContentFeedback.Disliked);

        var after = Recommender.Select(
            candidates, context, PosteriorMeans(profile));

        Assert.Equal(ShownRecommendedCount, after.Count);
        Assert.DoesNotContain(after, item => item.Id == rejected.Id);
    }

    /// <summary>
    /// Ankette işaretlenen etiket listenin başına gelmeli — kullanıcının
    /// "tercihlerime göre sıralanmıyor" şikâyeti buydu.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllPhases))]
    public void SurveyPreference_RanksMatchingItemsFirst(CyclePhase phase)
    {
        var candidates = CandidatesOf(phase, ContentType.FoodDo);

        // Her fazda bulunan ve kısıt taşımayan bir etiket seçilir.
        const TasteTag favourite = TasteTag.Vegetables;

        var profile = UserTasteProfile.CreateFor(Guid.NewGuid());
        profile.ApplySurvey(liked: [favourite], disliked: []);

        var selected = Recommender.Select(
            candidates,
            new RecommendationContext { Count = ShownRecommendedCount },
            PosteriorMeans(profile));

        Assert.All(selected, item =>
            Assert.True(
                TasteTags.Contains(item.TagMask, favourite),
                $"'{item.Title}' {favourite} sevdiğini söyleyen kullanıcının "
                + "listesinde ilk sıralarda çıkmamalı."));
    }

    /// <summary>Süreler makul aralıkta ve süre çiplerine oturuyor olmalı.</summary>
    [Fact]
    public void ExerciseDurations_StayInAPlausibleRange()
    {
        Assert.All(
            ContentSeedData.All.Where(item =>
                item.Type == ContentType.ExerciseDo),
            item => Assert.InRange(item.DurationMinutes!.Value, 5, 90));
    }

    /// <summary>Her öğede gerekçe var: kullanıcı neden sorusuna cevap görür.</summary>
    [Fact]
    public void EveryItem_HasATitleAndAReason()
    {
        Assert.All(ContentSeedData.All, item =>
        {
            Assert.False(string.IsNullOrWhiteSpace(item.Title));
            Assert.False(string.IsNullOrWhiteSpace(item.Body));
        });
    }

    private static (ContentType Type, int Shown)[] ShownCounts =>
    [
        (ContentType.FoodDo, ShownRecommendedCount),
        (ContentType.FoodAvoid, ShownLimitedCount),
        (ContentType.ExerciseDo, ShownRecommendedCount),
        (ContentType.ExerciseAvoid, ShownLimitedCount)
    ];

    private static bool IsRecommendation(ContentItem item) =>
        item.Type is ContentType.FoodDo or ContentType.ExerciseDo;

    private static IReadOnlyList<ContentItem> CandidatesOf(
        CyclePhase phase,
        ContentType type) =>
        [.. Catalog.Where(item => item.Phase == phase && item.Type == type)];

    /// <summary>
    /// Etiket başına <c>α / (α + β)</c>. Thompson örneklemesinin beklenen
    /// değeri; rastgelelik olmadan sıralamayı sınamayı sağlar.
    /// </summary>
    private static double[] PosteriorMeans(UserTasteProfile profile)
    {
        var means = new double[TasteTags.Count];

        for (var index = 0; index < means.Length; index++)
        {
            means[index] = (double)profile.Alpha[index]
                / (profile.Alpha[index] + profile.Beta[index]);
        }

        return means;
    }
}
