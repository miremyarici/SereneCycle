/// Backend DTO'larının Dart karşılıkları. Backend enum'ları
/// JsonStringEnumConverter ile metin olarak gönderiyor.
library;

import 'dart:convert';

enum CyclePhase {
  menstrual,
  follicular,
  ovulation,
  luteal;

  static CyclePhase fromJson(String value) => switch (value.toLowerCase()) {
        'menstrual' => CyclePhase.menstrual,
        'follicular' => CyclePhase.follicular,
        'ovulation' => CyclePhase.ovulation,
        _ => CyclePhase.luteal,
      };
}

class UserSummary {
  const UserSummary({
    required this.id,
    required this.name,
    required this.email,
    required this.emailConfirmed,
    required this.avgCycleLength,
    required this.avgPeriodLength,
    required this.hasCompletedOnboarding,
    this.avatarUpdatedAt,
  });

  factory UserSummary.fromJson(Map<String, dynamic> json) => UserSummary(
        id: json['id'] as String,
        name: json['name'] as String,
        email: json['email'] as String,
        emailConfirmed: json['emailConfirmed'] as bool,
        avgCycleLength: json['avgCycleLength'] as int,
        avgPeriodLength: json['avgPeriodLength'] as int,
        hasCompletedOnboarding: json['hasCompletedOnboarding'] as bool,
        avatarUpdatedAt: switch (json['avatarUpdatedAt']) {
          final String value => DateTime.parse(value),
          _ => null,
        },
      );

  final String id;
  final String name;
  final String email;
  final bool emailConfirmed;
  final int avgCycleLength;
  final int avgPeriodLength;
  final bool hasCompletedOnboarding;

  /// Profil fotoğrafı yoksa null. Fotoğrafın kendisi ayrı bir istekle
  /// (`GET /me/avatar`) indirilir; bu alan önbellek anahtarıdır.
  final DateTime? avatarUpdatedAt;

  bool get hasAvatar => avatarUpdatedAt != null;

  /// Her çağrıda yeniden derlenmesin diye sınıf düzeyinde: [initials] profil
  /// ekranında her build'de okunuyor.
  static final _whitespace = RegExp(r'\s+');

  /// Profil ekranındaki avatar için baş harfler.
  String get initials {
    final parts =
        name.trim().split(_whitespace).where((p) => p.isNotEmpty).toList();

    if (parts.isEmpty) return '?';
    if (parts.length == 1) return parts.first.substring(0, 1).toUpperCase();
    return (parts.first.substring(0, 1) + parts.last.substring(0, 1))
        .toUpperCase();
  }
}

class AuthResponse {
  const AuthResponse({
    required this.accessToken,
    required this.refreshToken,
    required this.user,
  });

  factory AuthResponse.fromJson(Map<String, dynamic> json) => AuthResponse(
        accessToken: json['accessToken'] as String,
        refreshToken: json['refreshToken'] as String,
        user: UserSummary.fromJson(json['user'] as Map<String, dynamic>),
      );

  final String accessToken;
  final String refreshToken;
  final UserSummary user;
}

/// `GET /me/export` yanıtı: kullanıcının sunucudaki bütün verisi.
///
/// Belge alan alan eşlenmiyor, olduğu gibi taşınıyor: dışa aktarımın amacı
/// veriyi eksiksiz teslim etmek. Alan alan eşleseydik sunucuya eklenen her
/// yeni alan, istemci güncellenene kadar dosyadan sessizce düşerdi.
class UserDataExport {
  const UserDataExport({
    required this.raw,
    required this.exportedAt,
    required this.cycleCount,
    required this.dailyLogCount,
  });

  factory UserDataExport.fromJson(Map<String, dynamic> json) => UserDataExport(
        raw: json,
        exportedAt: DateTime.parse(json['exportedAt'] as String),
        cycleCount: (json['cycles'] as List<dynamic>?)?.length ?? 0,
        dailyLogCount: (json['dailyLogs'] as List<dynamic>?)?.length ?? 0,
      );

  final Map<String, dynamic> raw;
  final DateTime exportedAt;

  /// Ekranda gösterilen özet: kullanıcı dosyayı açmadan ne indirdiğini
  /// görebilsin.
  final int cycleCount;
  final int dailyLogCount;

  /// Dosyaya yazılan ve panoya kopyalanan hâli. Girintili: dışa aktarım
  /// yalnızca makineler için değil, kullanıcının kendisi için de.
  String toPrettyJson() => const JsonEncoder.withIndent('  ').convert(raw);

  /// Tarih dosya adında: arka arkaya alınan dışa aktarımlar birbirinin
  /// üzerine yazmasın.
  String get fileName =>
      'serene-cycle-verilerim-'
      '${exportedAt.toIso8601String().substring(0, 10)}.json';
}

class CalendarDay {
  const CalendarDay({
    required this.date,
    required this.cycleDay,
    required this.phase,
    required this.isToday,
    required this.isPeriodDay,
    required this.hasLog,
    required this.hasBleeding,
    required this.hasSpotting,
    this.bloodColor,
  });

  factory CalendarDay.fromJson(Map<String, dynamic> json) => CalendarDay(
        date: DateTime.parse(json['date'] as String),
        cycleDay: json['cycleDay'] as int,
        phase: CyclePhase.fromJson(json['phase'] as String),
        isToday: json['isToday'] as bool,
        isPeriodDay: json['isPeriodDay'] as bool,
        hasLog: json['hasLog'] as bool,
        hasBleeding: json['hasBleeding'] as bool? ?? false,
        bloodColor: BloodColorOption.fromJson(json['bloodColor'] as String?),
        hasSpotting: json['hasSpotting'] as bool? ?? false,
      );

  final DateTime date;
  final int cycleDay;
  final CyclePhase phase;
  final bool isToday;

  /// Tahmine dayalı adet günü. Kullanıcının gerçekten kanama kaydettiği gün
  /// [hasBleeding] ile ayrı taşınır.
  final bool isPeriodDay;
  final bool hasLog;
  final bool hasBleeding;

  /// Kaydedilen kan rengi; takvimdeki damla buna göre boyanır. Kanama
  /// işaretli olup renk seçilmediyse null.
  final BloodColorOption? bloodColor;
  final bool hasSpotting;
}

/// Aylık takvim ekranının verisi.
class CalendarMonth {
  const CalendarMonth({
    required this.year,
    required this.month,
    required this.days,
  });

  factory CalendarMonth.fromJson(Map<String, dynamic> json) => CalendarMonth(
        year: json['year'] as int,
        month: json['month'] as int,
        days: (json['days'] as List<dynamic>)
            .map((e) => CalendarDay.fromJson(e as Map<String, dynamic>))
            .toList(),
      );

  final int year;
  final int month;
  final List<CalendarDay> days;
}

/// Backend'in `FlowIntensity` enum'u. `none` gönderilmez: kanama yoksa
/// alan boş bırakılır.
enum FlowLevel {
  light('Light', 'Az'),
  medium('Medium', 'Orta'),
  heavy('Heavy', 'Çok');

  const FlowLevel(this.wireValue, this.label);

  final String wireValue;
  final String label;

  static final _byWireValue = {
    for (final level in FlowLevel.values) level.wireValue.toLowerCase(): level,
  };

  static FlowLevel? fromJson(String? value) =>
      value == null ? null : _byWireValue[value.toLowerCase()];
}

enum BloodColorOption {
  red('Red', 'Kırmızı'),
  brown('Brown', 'Kahverengi'),
  pink('Pink', 'Pembe'),
  black('Black', 'Siyah'),
  orange('Orange', 'Turuncu'),
  gray('Gray', 'Gri');

  const BloodColorOption(this.wireValue, this.label);

  final String wireValue;
  final String label;

  static final _byWireValue = {
    for (final option in BloodColorOption.values)
      option.wireValue.toLowerCase(): option,
  };

  static BloodColorOption? fromJson(String? value) =>
      value == null ? null : _byWireValue[value.toLowerCase()];
}

/// Adet kaydı ekranındaki tek bir belirti çipi.
class SymptomOption {
  const SymptomOption({required this.id, required this.name});

  factory SymptomOption.fromJson(Map<String, dynamic> json) => SymptomOption(
        id: json['id'] as int,
        name: json['name'] as String,
      );

  final int id;
  final String name;
}

/// Bir güne ait kayıt. Kayıt yoksa backend boş varsayılanlarla döner.
class DailyLogEntry {
  const DailyLogEntry({
    required this.date,
    required this.hasBleeding,
    required this.flow,
    required this.bloodColor,
    required this.hasSpotting,
    required this.symptomIds,
    required this.note,
  });

  factory DailyLogEntry.fromJson(Map<String, dynamic> json) => DailyLogEntry(
        date: DateTime.parse(json['date'] as String),
        hasBleeding: json['hasBleeding'] as bool,
        flow: FlowLevel.fromJson(json['flow'] as String?),
        bloodColor: BloodColorOption.fromJson(json['bloodColor'] as String?),
        hasSpotting: json['hasSpotting'] as bool,
        symptomIds: (json['symptomIds'] as List<dynamic>)
            .map((e) => e as int)
            .toSet(),
        note: json['note'] as String?,
      );

  final DateTime date;
  final bool hasBleeding;
  final FlowLevel? flow;
  final BloodColorOption? bloodColor;
  final bool hasSpotting;
  final Set<int> symptomIds;
  final String? note;
}

/// Risk kartının genel seviyesi; kartın rengini ve ikonunu belirler.
enum RiskLevel {
  none,
  info,
  attention;

  static RiskLevel fromJson(String value) => switch (value.toLowerCase()) {
        'attention' => RiskLevel.attention,
        'info' => RiskLevel.info,
        _ => RiskLevel.none,
      };
}

/// Risk kartındaki tek satır. Başlık ve açıklama sunucudan gelir: eşikler ve
/// dil tek yerde (backend) dursun, iki taraf ayrışmasın.
class RiskFlag {
  const RiskFlag({
    required this.code,
    required this.level,
    required this.title,
    required this.detail,
  });

  factory RiskFlag.fromJson(Map<String, dynamic> json) => RiskFlag(
        code: json['code'] as String,
        level: RiskLevel.fromJson(json['level'] as String),
        title: json['title'] as String,
        detail: json['detail'] as String,
      );

  final String code;
  final RiskLevel level;
  final String title;
  final String detail;
}

/// Kartın alt şeridindeki nötr sayılar.
class RiskStats {
  const RiskStats({
    required this.loggedDays,
    required this.bleedingDays,
    required this.spottingDays,
    required this.painDays,
  });

  factory RiskStats.fromJson(Map<String, dynamic> json) => RiskStats(
        loggedDays: json['loggedDays'] as int,
        bleedingDays: json['bleedingDays'] as int,
        spottingDays: json['spottingDays'] as int,
        painDays: json['painDays'] as int,
      );

  final int loggedDays;
  final int bleedingDays;
  final int spottingDays;
  final int painDays;
}

/// Bu döngünün risk özeti. `/phase/today` yanıtının içinde gelir.
class CycleRisk {
  const CycleRisk({
    required this.level,
    required this.title,
    required this.message,
    required this.flags,
    required this.stats,
    required this.disclaimer,
  });

  factory CycleRisk.fromJson(Map<String, dynamic> json) => CycleRisk(
        level: RiskLevel.fromJson(json['level'] as String),
        title: json['title'] as String,
        message: json['message'] as String,
        flags: (json['flags'] as List<dynamic>)
            .map((e) => RiskFlag.fromJson(e as Map<String, dynamic>))
            .toList(),
        stats: RiskStats.fromJson(json['stats'] as Map<String, dynamic>),
        disclaimer: json['disclaimer'] as String,
      );

  final RiskLevel level;
  final String title;
  final String message;
  final List<RiskFlag> flags;
  final RiskStats stats;
  final String disclaimer;
}

class PhaseToday {
  const PhaseToday({
    required this.phase,
    required this.phaseName,
    required this.phaseDescription,
    required this.cycleDay,
    required this.cycleLength,
    required this.cycleStartDate,
    required this.predictedNextPeriod,
    required this.predictedOvulation,
    required this.isIrregular,
    required this.isPeriodLate,
    required this.commonMoods,
    required this.calendarStrip,
    required this.risk,
  });

  factory PhaseToday.fromJson(Map<String, dynamic> json) => PhaseToday(
        phase: CyclePhase.fromJson(json['phase'] as String),
        phaseName: json['phaseName'] as String,
        phaseDescription: json['phaseDescription'] as String,
        cycleDay: json['cycleDay'] as int,
        cycleLength: json['cycleLength'] as int,
        cycleStartDate: DateTime.parse(json['cycleStartDate'] as String),
        predictedNextPeriod:
            DateTime.parse(json['predictedNextPeriod'] as String),
        predictedOvulation:
            DateTime.parse(json['predictedOvulation'] as String),
        isIrregular: json['isIrregular'] as bool,
        isPeriodLate: json['isPeriodLate'] as bool,
        commonMoods: (json['commonMoods'] as List<dynamic>)
            .map((e) => e as String)
            .toList(),
        calendarStrip: (json['calendarStrip'] as List<dynamic>)
            .map((e) => CalendarDay.fromJson(e as Map<String, dynamic>))
            .toList(),
        risk: CycleRisk.fromJson(json['risk'] as Map<String, dynamic>),
      );

  final CyclePhase phase;
  final String phaseName;
  final String phaseDescription;
  final int cycleDay;
  final int cycleLength;

  /// İçinde bulunulan döngünün ilk günü — döngü ayarları ekranında
  /// "son adet başlangıcı" alanını doldurmak için.
  final DateTime cycleStartDate;
  final DateTime predictedNextPeriod;
  final DateTime predictedOvulation;
  final bool isIrregular;
  final bool isPeriodLate;
  final List<String> commonMoods;
  final List<CalendarDay> calendarStrip;

  /// Bu döngünün risk özeti; ana sayfadaki risk kartını besler.
  final CycleRisk risk;

  /// İlerleme halkasının doluluk oranı (0-1 arası).
  double get progress =>
      cycleLength == 0 ? 0 : (cycleDay / cycleLength).clamp(0.0, 1.0);
}

class ContentItem {
  const ContentItem({
    required this.id,
    required this.title,
    required this.body,
    this.durationMinutes,
  });

  factory ContentItem.fromJson(Map<String, dynamic> json) => ContentItem(
        id: json['id'] as int,
        title: json['title'] as String,
        body: json['body'] as String,
        durationMinutes: json['durationMinutes'] as int?,
      );

  /// Geri bildirim uç noktasının anahtarı.
  final int id;
  final String title;
  final String body;

  /// Yaklaşık süre; yalnızca hareket önerilerinde dolu.
  final int? durationMinutes;
}

/// Bir öneriye verilen tepki. Backend'de tek uca (`POST
/// /content/{id}/feedback`) gider; "tamamladım" beğeniden ayrı bir eksen
/// olduğu için burada da ayrı tutulur.
enum ContentReaction { liked, disliked }

/// Zevk anketinin gruplanması: kullanıcıya 24 çip tek yığın hâlinde
/// gösterilmesin diye.
enum TasteTagGroup {
  food('Neleri yemeyi seversin?'),
  exercise('Hangi hareketler sana iyi gelir?');

  const TasteTagGroup(this.title);

  final String title;
}

/// Öneri motorunun öğrendiği zevk etiketleri. Wire değerleri backend'deki
/// `TasteTag` adlarıdır; enum sıralaması wire'a girmez.
enum TasteTagOption {
  leafyGreens('LeafyGreens', 'Yeşil yapraklılar', TasteTagGroup.food),
  legumes('Legumes', 'Baklagiller', TasteTagGroup.food),
  wholeGrains('WholeGrains', 'Tam tahıllar', TasteTagGroup.food),
  redMeat('RedMeat', 'Kırmızı et', TasteTagGroup.food),
  poultry('Poultry', 'Beyaz et', TasteTagGroup.food),
  fish('Fish', 'Balık', TasteTagGroup.food),
  eggs('Eggs', 'Yumurta', TasteTagGroup.food),
  dairy('Dairy', 'Süt ürünleri', TasteTagGroup.food),
  nutsAndSeeds('NutsAndSeeds', 'Kuruyemiş ve tohum', TasteTagGroup.food),
  fruit('Fruit', 'Meyve', TasteTagGroup.food),
  vegetables('Vegetables', 'Sebze', TasteTagGroup.food),
  fermented('Fermented', 'Fermente', TasteTagGroup.food),
  sweet('Sweet', 'Tatlı', TasteTagGroup.food),
  spicy('Spicy', 'Baharatlı', TasteTagGroup.food),
  lightSoup('LightSoup', 'Hafif çorba', TasteTagGroup.food),
  yoga('Yoga', 'Yoga', TasteTagGroup.exercise),
  pilates('Pilates', 'Pilates', TasteTagGroup.exercise),
  cardio('Cardio', 'Kardiyo', TasteTagGroup.exercise),
  strength('Strength', 'Kuvvet', TasteTagGroup.exercise),
  hiit('Hiit', 'HIIT', TasteTagGroup.exercise),
  walking('Walking', 'Yürüyüş', TasteTagGroup.exercise),
  stretching('Stretching', 'Esneme', TasteTagGroup.exercise),
  dance('Dance', 'Dans', TasteTagGroup.exercise),
  outdoor('Outdoor', 'Açık hava', TasteTagGroup.exercise);

  const TasteTagOption(this.wireValue, this.label, this.group);

  final String wireValue;
  final String label;
  final TasteTagGroup group;

  /// Onboarding her build'de grupları soruyor; tarama bir kez yapılıp
  /// sonuç saklanıyor.
  static final Map<TasteTagGroup, List<TasteTagOption>> _byGroup = {
    for (final group in TasteTagGroup.values)
      group: [
        for (final option in TasteTagOption.values)
          if (option.group == group) option,
      ],
  };

  static List<TasteTagOption> inGroup(TasteTagGroup group) =>
      _byGroup[group] ?? const [];
}

enum AvoidFlagGroup {
  diet('Yiyemediklerin ya da tercih etmediklerin'),
  health('Dikkat etmen gerekenler'),
  equipment('Elinde olmayan ekipmanlar');

  const AvoidFlagGroup(this.title);

  final String title;
}

/// Kesin bilinen kısıtlar. Bunlar öğrenilmez: işaretlenen her bayrak,
/// ilgili önerileri liste hiç kurulmadan aday kümesinden çıkarır.
enum AvoidFlagOption {
  gluten('Gluten', 'Gluten', AvoidFlagGroup.diet),
  lactose('Lactose', 'Laktoz', AvoidFlagGroup.diet),
  treeNuts('TreeNuts', 'Fındık ve fıstık', AvoidFlagGroup.diet),
  shellfish('Shellfish', 'Kabuklu deniz ürünleri', AvoidFlagGroup.diet),
  eggAllergy('EggAllergy', 'Yumurta', AvoidFlagGroup.diet),
  vegetarian('Vegetarian', 'Vejetaryen', AvoidFlagGroup.diet),
  vegan('Vegan', 'Vegan', AvoidFlagGroup.diet),
  knee('Knee', 'Diz', AvoidFlagGroup.health),
  back('Back', 'Bel', AvoidFlagGroup.health),
  shoulder('Shoulder', 'Omuz', AvoidFlagGroup.health),
  pregnancy('Pregnancy', 'Hamilelik', AvoidFlagGroup.health),
  equipmentDumbbell('EquipmentDumbbell', 'Dambıl', AvoidFlagGroup.equipment),
  equipmentMat('EquipmentMat', 'Mat', AvoidFlagGroup.equipment),
  equipmentGym('EquipmentGym', 'Spor salonu', AvoidFlagGroup.equipment);

  const AvoidFlagOption(this.wireValue, this.label, this.group);

  final String wireValue;
  final String label;
  final AvoidFlagGroup group;

  /// Bkz. [TasteTagOption.inGroup]: gruplama bir kez hesaplanıyor.
  static final Map<AvoidFlagGroup, List<AvoidFlagOption>> _byGroup = {
    for (final group in AvoidFlagGroup.values)
      group: [
        for (final option in AvoidFlagOption.values)
          if (option.group == group) option,
      ],
  };

  static List<AvoidFlagOption> inGroup(AvoidFlagGroup group) =>
      _byGroup[group] ?? const [];
}

class PhaseContent {
  const PhaseContent({
    required this.phaseName,
    required this.recommended,
    required this.limited,
    required this.disclaimer,
  });

  factory PhaseContent.fromJson(Map<String, dynamic> json) => PhaseContent(
        phaseName: json['phaseName'] as String,
        recommended: (json['recommended'] as List<dynamic>)
            .map((e) => ContentItem.fromJson(e as Map<String, dynamic>))
            .toList(),
        limited: (json['limited'] as List<dynamic>)
            .map((e) => ContentItem.fromJson(e as Map<String, dynamic>))
            .toList(),
        disclaimer: json['disclaimer'] as String,
      );

  final String phaseName;
  final List<ContentItem> recommended;
  final List<ContentItem> limited;
  final String disclaimer;
}
