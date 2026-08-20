using System.Numerics;

namespace SereneCycle.Domain.Content;

/// <summary>
/// Zevk sözlüğü: öneri motorunun öğrendiği tek yüzey. Kollar öğe değil
/// etikettir — 500 öğede öğe başına bir kol açmak günde ~1 geri bildirim
/// veren kullanıcıda yıllar sürerdi, ~24 etikete taşıyınca aynı hedef
/// aylara iner.
///
/// Sıra numaraları maskedeki bit indeksidir ve zevk vektöründeki dizinin
/// indeksi olarak da kullanılır: <b>var olan bir değerin sayısı asla
/// değişmemeli</b>, yeni etiket yalnızca sona eklenir.
/// </summary>
public enum TasteTag
{
    // --- Yiyecek ---
    LeafyGreens = 0,
    Legumes = 1,
    WholeGrains = 2,
    RedMeat = 3,
    Poultry = 4,
    Fish = 5,
    Eggs = 6,
    Dairy = 7,
    NutsAndSeeds = 8,
    Fruit = 9,
    Vegetables = 10,
    Fermented = 11,
    Sweet = 12,
    Spicy = 13,
    LightSoup = 14,

    // --- Egzersiz ---
    Yoga = 15,
    Pilates = 16,
    Cardio = 17,
    Strength = 18,
    Hiit = 19,
    Walking = 20,
    Stretching = 21,
    Dance = 22,
    Outdoor = 23
}

/// <summary>
/// Kısıt sözlüğü. Semantik ters uygulanmaya çok müsait: bayrak öğenin
/// üzerindedir ve <b>öğeyi eleyecek kullanıcı bayraklarını</b> beyan eder.
///
/// Örnek: <c>Somon.ContraMask = {Vegetarian, Vegan}</c>. Vejetaryen
/// kullanıcının <c>AvoidMask</c>'inde <c>Vegetarian</c> biti vardır, bu
/// yüzden <c>(item.ContraMask &amp; user.AvoidMask) != 0</c> olur ve öğe
/// elenir. Ekipman da aynı eksende: dambıl isteyen egzersiz
/// <c>EquipmentDumbbell</c> işaretlenir, dambılı olmayan kullanıcı aynı
/// biti <c>AvoidMask</c>'inde taşır.
/// </summary>
public enum ContraTag
{
    // --- Beslenme ---
    Gluten = 0,
    Lactose = 1,
    TreeNuts = 2,
    Shellfish = 3,
    EggAllergy = 4,
    Vegetarian = 5,
    Vegan = 6,

    // --- Sağlık ---
    Knee = 7,
    Back = 8,
    Shoulder = 9,
    Pregnancy = 10,

    // --- Ekipman ---
    EquipmentDumbbell = 11,
    EquipmentMat = 12,
    EquipmentGym = 13
}

/// <summary>
/// <see cref="TasteTag"/> maskesinin bit yardımcıları — <c>SymptomMasks</c>
/// ile aynı desen.
/// </summary>
public static class TasteTags
{
    /// <summary>
    /// Zevk vektörünün (α/β dizilerinin) uzunluğu. Maske 64 bitlik olduğu
    /// için yeni etiket eklemek migration gerektirmez.
    /// </summary>
    public const int Capacity = 64;

    /// <summary>Şu an tanımlı etiket sayısı — örnekleme bu kadarıyla sınırlı.</summary>
    public static readonly int Count = Enum.GetValues<TasteTag>().Length;

    public static long BitOf(TasteTag tag) => 1L << (int)tag;

    public static long Of(IEnumerable<TasteTag> tags)
    {
        ArgumentNullException.ThrowIfNull(tags);

        var mask = 0L;

        foreach (var tag in tags)
        {
            mask |= BitOf(tag);
        }

        return mask;
    }

    public static bool Contains(long mask, TasteTag tag) =>
        (mask & BitOf(tag)) != 0;

    /// <summary>Maskede set olan etiketlerin indeksleri; tahsis yapmaz.</summary>
    public static TagIndexEnumerator IndicesOf(long mask) => new(mask);
}

/// <summary>
/// <see cref="ContraTag"/> maskesinin bit yardımcıları. Zevk sözlüğüyle
/// ayrı bit uzayları: ikisi karıştırılırsa bir alerji bayrağı zevk kolu
/// gibi öğrenilmeye başlar.
/// </summary>
public static class ContraTags
{
    public static readonly IReadOnlyList<ContraTag> All =
        Enum.GetValues<ContraTag>();

    public static long BitOf(ContraTag tag) => 1L << (int)tag;

    public static long Of(IEnumerable<ContraTag> tags)
    {
        ArgumentNullException.ThrowIfNull(tags);

        var mask = 0L;

        foreach (var tag in tags)
        {
            mask |= BitOf(tag);
        }

        return mask;
    }

    public static bool Contains(long mask, ContraTag tag) =>
        (mask & BitOf(tag)) != 0;
}

/// <summary>
/// Maskede set olan bit indekslerini gezer. Sınıf değil struct ve
/// <c>IEnumerable</c> uygulamıyor: skorlama yolunda aday öğe başına bir kez
/// çalışıyor, tahsis yapmaması önemli.
/// </summary>
public struct TagIndexEnumerator(long mask)
{
    private long _remaining = mask;

    public int Current { get; private set; }

    public readonly TagIndexEnumerator GetEnumerator() => this;

    public bool MoveNext()
    {
        if (_remaining == 0)
        {
            return false;
        }

        Current = BitOperations.TrailingZeroCount(_remaining);

        // En düşük set biti söndür — kalan bit sayısı kadar döner.
        _remaining &= _remaining - 1;

        return true;
    }
}
