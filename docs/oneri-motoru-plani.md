# Görev: Faza göre beslenme ve egzersiz öneri motoru

Bu bir uygulama görevidir. Aşağıdaki tasarım tartışılıp kararlaştırıldı; **gerekçeleriyle
birlikte verilen kararları değiştirme**, yalnızca uygula.

---

## 1. Proje bağlamı

**SereneCycle** — menstrüel döngü takip uygulaması, portföy odaklı, Android hedefli.

- **Backend:** ASP.NET Core (net9.0), EF Core + PostgreSQL, katmanlı mimari:
  `SereneCycle.Domain` → `SereneCycle.Application` → `SereneCycle.Infrastructure` → `SereneCycle.Api`
- **Mobil:** Flutter + Riverpod + go_router (`mobile/`)
- **Dil:** Kod yorumları ve kullanıcıya görünen tüm metinler **Türkçe**.
- **Testler:** xUnit (`backend/tests/SereneCycle.Tests/`), Flutter widget testleri (`mobile/test/`)

### Uyman gereken mevcut desenler

| Desen | Referans dosya |
|---|---|
| Saf domain mantığı (I/O yok, `DateTime.Now` yok, dışarıdan `today`) | `Domain/Cycles/PhaseCalculator.cs` |
| Saf kural motoru, eşikler tek yerde | `Domain/Risk/RiskEvaluator.cs` |
| Bitmask ile O(1) küme testi | `Domain/Risk/CycleRiskSignals.cs` (`SymptomMasks`) |
| Kullanıcı başına tek satır, PK ile O(1) okuma | `Domain/Entities/CycleRiskSummary.cs` |
| Sunum metinlerinin domain'den ayrılması | `Application/Risk/RiskContent.cs` |
| Servis dönüş tipi | `Application/Common/Result<T>` |
| EF yapılandırması | `Infrastructure/Persistence/Configurations/*.cs` |

Enum'lar veritabanına **metin** olarak yazılır (`HasConversion<string>()`), API'de
`JsonStringEnumConverter` ile metin döner. Tablo adları snake_case.

---

## 2. Çözülecek problem

`ContentService` şu an o fazın **bütün** içeriğini `OrderBy(Id)` ile döndürüyor.
Katalogda toplam **29 öğe** var:

| Faz | Yiyecek (öncelik ver) | Yiyecek (sınırlı) | Egzersiz |
|---|---|---|---|
| Menstrüel | 3 | 2 | 3 |
| Foliküler | 3 | 1 | 3 |
| Ovulasyon | 2 | 1 | 3 |
| Luteal | 3 | 2 | 3 |

Kullanıcı, fazın 14 günü boyunca her açılışta **aynı 3 yiyeceği** görüyor.

**Teşhis:** Çeşitlilik tavanını katalog boyutu belirler, sıralama algoritması değil.
Öneri motoru bir sıralama fonksiyonudur; 3 öğeyi farklı sıralarsan yine 3 öğe görürsün.
İş iki bağımsız düğmeye ayrılır:

- **Tavan** = katalog boyutu → veri girme işi (hedef: faz başına 100-150, toplam 400-600)
- **Tazelik hissi** = örnekleme + tekrar engelleme → birkaç satır kod

---

## 3. Değiştirilmeyecek tasarım kararları

### 3.1. Makine yalnızca tek bir şey öğrenir

- **Kuralla kesin bilinir → öğrenme YOK:** alerji, diyet, sakatlık, hamilelik, ekipman,
  mevcut süre, hava durumu, faz, son gösterilenler.
- **Bilinemez → model ŞART:** bu kullanıcı bu *türden* şeyleri seviyor mu.

Öğrenilen yüzey bu kadar: **kullanıcı başına tek bir zevk vektörü.**

### 3.2. Kollar öğe değil, ETİKET

500 öğede öğe başına bir kol açarsan, kullanıcı günde ~1 geri bildirim verirken kol
başına 5 gözleme ulaşmak yıllar sürer. Kolları ~24 etikete taşıyınca aynı hedef
**3-6 aya** iner. Tasarımın belkemiği budur.

### 3.3. Güvenlik = sert filtre, "constrained bandit" DEĞİL

Kısıtlar (alerji, sakatlık, hamilelik) baştan **kesin** biliniyor. Kısıtlı bandit
algoritmaları kısıt sağlamayı *yüksek olasılıkla* garanti eder; sağlıkta bu yetmez.
Yasaklı öğe, bandit onu görmeden aday kümesinden çıkarılır — garanti istatistiksel
değil **kesin** olur ve birim testle kanıtlanabilir.

### 3.4. Bağlam öğrenilmez, filtrelenir

"Sabah + az vakit → kısa antrenman" öğrenilecek bir şey değil, bilinen bir kısıttır.
Süre/ekipman/hava sert filtreye gider. Bağlam vektörü (LinTS) kurulmaz: ayrık LinTS
kullanıcı başına ~1.6 MB ister ve bu, "kullanıcı sayısı hesaba giriyor" tuzağıdır.

### 3.5. Ödül kısa vadeli ve atfedilebilir olur

Kilo/uzun vadeli hedef ödül olarak **kullanılmaz**. Üç sebep: (a) bu bir kilo verme
uygulaması değil, `AppUser`'da kilo alanı bile yok ve kadın sağlığı uygulamasında
yemek önerisini kilo kaybına göre optimize etmek bilinçli olarak istenmeyen bir üründür;
(b) atıf bozuk — 3 ayda gösterilen 200 öğünün hangisi 2 kiloyu açıklar; (c) döngü içi
su tutulumu tek başına 1-2 kg salınım yapar ve ödüle döngüyle senkron sahte sinyal enjekte eder.

### 3.6. Katalog LLM ile ÇEVRİMDIŞI üretilir

İçerik bir kez üretilir, insan denetiminden geçer, seed data olarak commit edilir.
Çalışma anında LLM çağrısı yoktur: ağ yok, gecikme yok, ücret yok, çevrimdışı çalışır,
deterministik ve test edilebilir. Tıbbi-yakın içerikte her maddenin yayına girmeden
insan gözünden geçmesi zorunludur.

---

## 4. Veri modeli

### 4.1. İki AYRI sözlük — karıştırma

**`TagMask` (zevk sözlüğü)** — bandit kollarıdır, kişinin istikrarlı tercihi olan şeyler:

- Yiyecek: `yeşil-yapraklı`, `baklagil`, `tam-tahıl`, `kırmızı-et`, `beyaz-et`, `balık`,
  `yumurta`, `süt-ürünü`, `kuruyemiş-tohum`, `meyve`, `sebze`, `fermente`, `tatlı`,
  `baharatlı`, `çorba-hafif`
- Egzersiz: `yoga`, `pilates`, `kardiyo`, `kuvvet`, `hiit`, `yürüyüş`, `esneme`, `dans`,
  `açık-hava`

**`ContraMask` (kısıt sözlüğü)** — öğenin "beni şu kullanıcı bayrakları eler" beyanı:

- `gluten`, `laktoz`, `fındık-fıstık`, `kabuklu-deniz`, `yumurta-alerjisi`,
  `vejetaryen`, `vegan`, `diz`, `bel`, `omuz`, `hamilelik`, `ekipman-dambıl`,
  `ekipman-mat`, `ekipman-salon`

**Semantik (ters uygulanmaya çok müsait, dikkat):** öğe, kendisini eleyecek bayrakları
işaretler.

> Örnek: `Somon` → `ContraMask = {vejetaryen, vegan, balık-alerjisi}`.
> Vejetaryen kullanıcının `AvoidMask`'inde `vejetaryen` biti vardır →
> `(item.ContraMask & user.AvoidMask) != 0` → elenir.

Ekipman aynı eksende: `Dambıl ile squat` → `ContraMask = {ekipman-dambıl}`; dambılı
olmayan kullanıcının `AvoidMask`'inde `ekipman-dambıl` biti set edilir.

### 4.2. Şema değişiklikleri

`ContentItem` (`Domain/Entities/ContentItem.cs`) — üç kolon ekle:

- `long TagMask`
- `long ContraMask`
- `int? DurationMinutes` (yalnızca egzersizde dolu)

`AppUser` (`Infrastructure/Identity/AppUser.cs`) — bir kolon ekle:

- `long AvoidMask`

Yeni tablo `user_taste_profiles` — `CycleRiskSummary` deseniyle aynı, kullanıcı başına
tek satır:

- `UserId` (PK)
- `short[] Alpha` (64 uzunluk, Postgres `smallint[]`)
- `short[] Beta` (64 uzunluk)
- `DateTimeOffset UpdatedAt`

Kullanıcı başına **256 bayt**, join yok, tek PK getirisi. Maske 64 bitlik ama v1'de
~24 etiket dolu — büyümek için migration gerekmiyor.

---

## 5. Algoritma

### 5.1. İstek anı (öneri listesi üretme)

```
1. Fazın adaylarını çek                             O(N/4), indeksli sorgu
2. Sert filtre:
     (item.ContraMask & user.AvoidMask) == 0        O(1) / öğe
     item.DurationMinutes <= mevcutSüre             O(1) / öğe
     son gösterilenler maskesinde değil             O(1) / öğe
3. Her etiket için θ_t ~ Beta(α_t, β_t)             O(T), T=24
4. Skor(item) = ortalama(θ_t : t ∈ item.TagMask)    O(popcount) ≈ 3
5. Sınırlı min-heap ile top-k                       O(m log k)
```

**3. adımda kritik detay:** θ örneklemesi **istek başına bir kez** yapılır, öğe başına
değil. Tek bir dünya hipotezi çekip ona göre açgözlü davranmak Thompson'ın doğru
formülasyonudur; öğe başına yeniden örneklemek etiketler arası korelasyonu bozar.

**Beta örnekleme:** .NET'te hazır Beta dağılımı yok, gerekmiyor da:
`Beta(α,β) = X/(X+Y)`, burada `X~Gamma(α,1)`, `Y~Gamma(β,1)`.
Marsaglia-Tsang gamma örnekleyicisi ~20 satır, beklenen O(1).
**Saf ve tohumlanabilir `Random` alacak** ki testler deterministik olsun.

### 5.2. Öğrenme

**Ödül** (`POST /content/{id}/feedback`):

- 👍 → öğenin her etiketi için `α += 2`
- 👎 → her etiket için `β += 2`
- "tamamladım" → `α += 1`

Güncelleme maliyeti `O(popcount(TagMask))` ≈ 3 artırma + tek satır yazma.

**Prior (soğuk başlangıç)** — onboarding anketinden:

```
α_t = 1 + s_t · C
β_t = 1 + (1 − s_t) · C        ,  C = 4
```

`C` sahte-sayımdır: "bu anket kaç gerçek gözleme bedel". `C=4` → kullanıcının 4 gerçek
geri bildiriminden sonra veri anketi bastırır. Ankette başla, gerçeğe doğru sön.

Ankette hangi sorunun nereye gittiğini ayır: *"Vejetaryen misin?"* bir zevk prior'ı
**değil**, `AvoidMask`'e giden bir kısıttır. *"Yoga sever misin?"* zevk prior'ıdır.

**Unutma (her güncellemede):**

```
α ← 1 + 0.99·(α − 1)
β ← 1 + 0.99·(β − 1)
```

Üç şeyi birden çözer: sayaçlar sonsuza büyümez (smallint taşmaz), posterior sonsuz
kesinliğe kilitlenmez (keşif hiç bitmez), zevk değişimi takip edilir.

---

## 6. Karmaşıklık bütçesi — İHLAL ETME

| İşlem | Zaman | Alan | Ne zaman |
|---|---|---|---|
| Katalog yükleme | O(N) | O(N) ≈ 60 KB, **süreç geneli** | uygulama açılışında bir kez |
| Zevk satırı okuma | O(1) | 256 B / kullanıcı | her öneri isteği |
| Beta örnekleme | O(T), T=24 | O(T) | her öneri isteği |
| Filtre + skor | O(N/4) | O(1) | her öneri isteği |
| Top-k | O(m log k) | O(k) | her öneri isteği |
| Geri bildirim | O(3) | O(1) | 👍/👎 basılınca |
| Prior kurulumu | O(T) | O(1) | kayıtta bir kez |

**Değişmez kural:** hiçbir satırda kullanıcı sayısı (U) veya kullanıcının toplam geçmiş
uzunluğu geçmeyecek. `RiskService`'teki disiplinin aynısı.

---

## 7. Bilinçli olarak YAPILMAYACAKLAR

- Bağlam vektörü / LinTS / bağlamsal bandit — bağlam sert filtreye gidiyor
- Bağlam kovaları (v1'de yok; faz zaten bölümleme yapıyor)
- Kilo veya uzun vadeli hedef temelli ödül
- İşbirlikçi filtreleme / kullanıcı benzerliği — O(U²) ve soğuk başlangıçta çalışmaz
- ML.NET, TFLite, model dosyası, eğitim döngüsü
- Çalışma anında LLM/API çağrısı
- Constrained bandit makinesi — kısıtlar kesin bilindiği için sert filtre daha güçlü
- Öneri sonucunu önbelleğe alma — hesap zaten mikrosaniye, önbellek geçersizleştirme
  derdi getirir

---

## 8. Uygulama sırası

Her adım tek başına çalışır bir ürün bırakmalı; yarım yolda durulabilir olması tasarımın amacı.

### Adım 1 — Etiket sözlüğü ve maske altyapısı

- `Domain/Content/ContentTags.cs`: iki ayrı sözlük (`TasteTags`, `ContraTags`),
  `SymptomMasks` deseninde bit yardımcıları
- `ContentItem`'a `TagMask`, `ContraMask`, `DurationMinutes`; `AppUser`'a `AvoidMask`
- EF yapılandırması + migration
- Mevcut 29 öğeyi yeni şemaya taşı (seeder'da maskeleri doldur)
- **Katalog büyütme bu adımda YOK** — ayrı adımda, editoryal onayla

### Adım 2 — Sert filtre + ağırlıklı seçim (bandit yok)

- `Domain/Content/Recommender.cs`: saf seçim fonksiyonu, dışarıdan `Random` alır
- Ağırlıklı rastgele seçim + son gösterilenleri dışlama
- **Günlük tohum:** `hash(userId, tarih, faz)` ile beslenen deterministik RNG →
  kullanıcı ekranı yenileyince liste zıplamaz, ertesi gün değişir; önbelleğe gerek kalmaz
- `ContentService` bunu kullanacak şekilde güncellenir

### Adım 3 — Geri bildirim + zevk profili

- `user_taste_profiles` tablosu + migration
- `POST /content/{id}/feedback` uç noktası (`{ "liked": true }`)
- Onboarding anketinden prior kurulumu
- Mobilde 👍/👎 düğmeleri

### Adım 4 — Beta-TS seçimi

- `Domain/Content/BetaSampler.cs` (Marsaglia-Tsang, saf, tohumlanabilir)
- `Recommender` skorlamasını θ örneklemesine geçir
- Unutma katsayısını güncellemeye ekle

---

## 9. Kabul kriterleri

**Birim testleri** (mevcut test stiline uy: Türkçe test adları ve yorumlar):

- Sert filtre: `AvoidMask`'i olan kullanıcının aday listesinde yasaklı öğe **asla**
  çıkmaz — bu istatistiksel değil kesin bir garanti, testi öyle yaz
- Ekipmanı olmayan kullanıcıya ekipman gerektiren egzersiz gelmez
- Süre kısıtı: 15 dakikası olan kullanıcıya 45 dakikalık egzersiz gelmez
- Aynı gün iki çağrı aynı listeyi verir; ertesi gün farklı verir (günlük tohum)
- Son gösterilenler tekrar edilmez
- `BetaSampler`: sabit tohumla deterministik; `Beta(1,1)` yaklaşık düzgün dağılır;
  `α >> β` iken örnekler 1'e yakınlaşır
- Prior: `C=4` iken 4 geri bildirimden sonra veri anketi bastırır
- Unutma: sayaçlar üst sınıra yakınsar, taşmaz

**Doğrulama:**

- `dotnet build` 0 uyarı, `dotnet test` tamamı yeşil
- `flutter analyze` temiz, `flutter test` tamamı yeşil
- Migration üretilmiş olacak; Development ortamı `MigrateAsync()` ile otomatik uygular

**İçerik kuralları:**

- Her öğede kısa gerekçe (`Body`) bulunur — cycle syncing'in kanıt tabanı zayıf-orta,
  bu yüzden dil kesinlik iddia etmez ("olabilir", "bazı kişilerde")
- "Yasak" değil "sınırlı tut" dili
- Ekranların altında `PhaseContent.MedicalDisclaimer` gösterilir
