import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:intl/date_symbol_data_local.dart';

import 'package:serene_cycle/core/api/models.dart';
import 'package:serene_cycle/core/api/serene_api.dart';
import 'package:serene_cycle/core/providers/app_providers.dart';
import 'package:serene_cycle/core/theme/app_theme.dart';
import 'package:serene_cycle/core/theme/blood_colors.dart';
import 'package:serene_cycle/features/exercise/presentation/exercise_screen.dart';
import 'package:serene_cycle/features/home/presentation/calendar_screen.dart';
import 'package:serene_cycle/features/home/presentation/home_screen.dart';
import 'package:serene_cycle/features/logs/presentation/period_log_screen.dart';
import 'package:serene_cycle/features/nutrition/presentation/nutrition_screen.dart';
import 'package:serene_cycle/features/profile/presentation/cycle_settings_screen.dart';
import 'package:serene_cycle/features/profile/presentation/privacy_screen.dart';
import 'package:serene_cycle/features/profile/presentation/profile_screen.dart';

/// Risk kartının "temiz" hali: işaret yok, yalnızca sayılar.
final _riskJson = <String, dynamic>{
  'level': 'None',
  'title': 'Dikkat çeken bir şey yok',
  'message': 'Bu döngüde 6 gün kaydettin ve kayıtlarında dikkat çeken bir '
      'örüntü görünmüyor.',
  'flags': <Map<String, dynamic>>[],
  'stats': {
    'loggedDays': 6,
    'bleedingDays': 5,
    'spottingDays': 1,
    'painDays': 2,
  },
  'disclaimer': 'Bu içerik bilgilendirme amaçlıdır, tıbbi tavsiye değildir.',
};

/// Backend'in gerçekten döndürdüğü şekle birebir uyan örnek yanıt.
final _phaseJson = <String, dynamic>{
  'phase': 'Follicular',
  'phaseName': 'Foliküler Faz',
  'phaseDescription':
      'Östrojen yükseliyor. Enerjinde ve yaratıcılığında bir canlanma '
          'hissedebilirsin.',
  'cycleDay': 7,
  'cycleLength': 28,
  'cycleStartDate': '2026-08-07',
  'predictedNextPeriod': '2026-09-04',
  'predictedOvulation': '2026-08-21',
  'isIrregular': false,
  'isPeriodLate': false,
  'commonMoods': ['Enerjik', 'Odaklı', 'Sakin'],
  'calendarStrip': [
    for (var i = 0; i < 7; i++)
      {
        'date': '2026-08-${(10 + i).toString().padLeft(2, '0')}',
        'cycleDay': 4 + i,
        'phase': i < 2 ? 'Menstrual' : 'Follicular',
        'isToday': i == 3,
        'isPeriodDay': i < 2,
        'hasLog': i == 2,
        'hasBleeding': i < 2,
        'hasSpotting': i == 2,
      },
  ],
  'risk': _riskJson,
};

/// Takvim ekranının açtığı ay: ekran `DateTime.now()`'dan türetiyor.
final _currentMonth = DateTime(DateTime.now().year, DateTime.now().month);

Map<String, dynamic> _calendarJson(DateTime month) {
  final dayCount = DateTime(month.year, month.month + 1, 0).day;

  return {
    'year': month.year,
    'month': month.month,
    'days': [
      for (var day = 1; day <= dayCount; day++)
        {
          'date': '${month.year.toString().padLeft(4, '0')}-'
              '${month.month.toString().padLeft(2, '0')}-'
              '${day.toString().padLeft(2, '0')}',
          'cycleDay': day,
          'phase': day <= 5 ? 'Menstrual' : 'Follicular',
          'isToday': false,
          'isPeriodDay': day <= 5,
          'hasLog': day <= 3,
          'hasBleeding': day <= 3,
          // Damla, kaydedilen kan rengine göre boyanıyor.
          'bloodColor': day <= 3 ? 'Brown' : null,
          'hasSpotting': day == 4,
        },
    ],
  };
}

final _symptomsJson = <Map<String, dynamic>>[
  {'id': 6, 'name': 'Akne'},
  {'id': 1, 'name': 'Karın krampları'},
  {'id': 5, 'name': 'Yorgunluk'},
];

final _dailyLogJson = <String, dynamic>{
  'date': '2026-08-10',
  'hasBleeding': true,
  'flow': 'Medium',
  'bloodColor': 'Red',
  'hasSpotting': false,
  'symptomIds': [1],
  'note': null,
};

final _nutritionJson = <String, dynamic>{
  'phase': 'Follicular',
  'phaseName': 'Foliküler Faz',
  'recommended': [
    {'id': 9, 'title': 'Yumurta', 'body': 'Kaliteli protein.'},
    {'id': 10, 'title': 'Avokado', 'body': 'Sağlıklı yağlar.'},
  ],
  'limited': [
    {'id': 12, 'title': 'İşlenmiş şeker', 'body': 'Enerji dalgalanması yapabilir.'},
  ],
  'disclaimer': 'Bu içerik bilgilendirme amaçlıdır, tıbbi tavsiye değildir.',
};

/// Geri bildirimden sonra sunucunun döndürdüğü liste: aynı fazın başka
/// adayları. Katalog gösterilen sayıdan geniş olduğu için mümkün.
final _nutritionAfterFeedbackJson = <String, dynamic>{
  'phase': 'Follicular',
  'phaseName': 'Foliküler Faz',
  'recommended': [
    {'id': 21, 'title': 'Kinoa', 'body': 'Glutensiz tam tahıl.'},
    {'id': 22, 'title': 'Badem', 'body': 'Magnezyum kaynağı.'},
  ],
  'limited': [
    {'id': 12, 'title': 'İşlenmiş şeker', 'body': 'Enerji dalgalanması yapabilir.'},
  ],
  'disclaimer': 'Bu içerik bilgilendirme amaçlıdır, tıbbi tavsiye değildir.',
};

final _exerciseJson = <String, dynamic>{
  'phase': 'Follicular',
  'phaseName': 'Foliküler Faz',
  'recommended': [
    {
      'id': 13,
      'title': 'Kardiyo',
      'body': 'Artan enerjiyi değerlendirir.',
      'durationMinutes': 30,
    },
  ],
  'limited': [
    {'id': 15, 'title': 'Uzun süreli hareketsizlik', 'body': 'Hareket iyi gelir.'},
  ],
  'disclaimer': 'Bu içerik bilgilendirme amaçlıdır, tıbbi tavsiye değildir.',
};

/// Süre filtresi hiçbir adayı geçirmediğinde gelen yanıt.
final _emptyExerciseJson = <String, dynamic>{
  'phase': 'Follicular',
  'phaseName': 'Foliküler Faz',
  'recommended': <Map<String, dynamic>>[],
  'limited': [
    {'id': 15, 'title': 'Uzun süreli hareketsizlik', 'body': 'Hareket iyi gelir.'},
  ],
  'disclaimer': 'Bu içerik bilgilendirme amaçlıdır, tıbbi tavsiye değildir.',
};

/// Yalnızca geri bildirim ucunu taklit eder; testin dokunduğu tek çağrı o.
/// Beklenmeyen bir çağrı sessizce geçmesin diye `noSuchMethod` hata atar.
class _FeedbackOnlyApi implements SereneApi {
  final List<int> feedbackForItems = [];

  @override
  Future<void> sendContentFeedback(
    int contentItemId, {
    bool? liked,
    bool completed = false,
  }) async {
    feedbackForItems.add(contentItemId);
  }

  @override
  dynamic noSuchMethod(Invocation invocation) => throw UnsupportedError(
        'Test bu çağrıyı beklemiyor: ${invocation.memberName}',
      );
}

final _userJson = <String, dynamic>{
  'id': '00000000-0000-0000-0000-000000000001',
  'name': 'Test Kullanıcı',
  'email': 'test@serenecycle.app',
  'emailConfirmed': true,
  'avgCycleLength': 28,
  'avgPeriodLength': 5,
  'hasCompletedOnboarding': true,
};

// Not: Riverpod 3.4'te `Override` tipi flutter_riverpod'dan dışa
// aktarılmıyor, bu yüzden ProviderScope çağrı yerinde kuruluyor.
Widget _app(Widget screen) =>
    MaterialApp(theme: AppTheme.light, home: screen);

Future<void> _pump(WidgetTester tester, Widget scoped) async {
  await tester.pumpWidget(scoped);
  await tester.pumpAndSettle();
}

/// Varsayılan 800x600 test yüzeyinde ListView alttaki kartları hiç
/// oluşturmuyor. Mantıksal boyutu telefon genişliğinde ama çok uzun
/// yaparak tüm kartların build edilmesini garantiliyoruz.
void _useTallScreen(WidgetTester tester) {
  tester.view.physicalSize = const Size(420, 2400);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);
}

void main() {
  setUpAll(() => initializeDateFormatting('tr'));

  testWidgets('Ana Sayfa faz, halka, takvim ve duyguları gösterir', (
    tester,
  ) async {
    _useTallScreen(tester);

    await _pump(
      tester,
      ProviderScope(
        overrides: [
          phaseTodayProvider.overrideWith(
            (ref) async => PhaseToday.fromJson(_phaseJson),
          ),
        ],
        child: _app(const HomeScreen()),
      ),
    );

    expect(find.text('Foliküler Faz'), findsOneWidget);
    expect(find.text('7. gün'), findsOneWidget);
    expect(find.text('28 günlük döngü'), findsOneWidget);
    expect(find.textContaining('Östrojen yükseliyor'), findsOneWidget);

    // Tahminler
    expect(find.text('Tahmini adet'), findsOneWidget);
    expect(find.text('Tahmini ovulasyon'), findsOneWidget);

    // Takvim şeridi: 7 gün
    expect(find.text('BU HAFTA'), findsOneWidget);
    for (final day in ['10', '11', '12', '13', '14', '15', '16']) {
      expect(find.text(day), findsOneWidget);
    }

    // Duygu çipleri
    expect(find.text('Enerjik'), findsOneWidget);
    expect(find.text('Odaklı'), findsOneWidget);
    expect(find.text('Sakin'), findsOneWidget);
  });

  testWidgets('Düzensiz döngüde güven uyarısı gösterilir', (tester) async {
    _useTallScreen(tester);

    final irregular = Map<String, dynamic>.from(_phaseJson)
      ..['isIrregular'] = true;

    await _pump(
      tester,
      ProviderScope(
        overrides: [
          phaseTodayProvider.overrideWith(
            (ref) async => PhaseToday.fromJson(irregular),
          ),
        ],
        child: _app(const HomeScreen()),
      ),
    );

    expect(find.textContaining('tahminlerin güveni düşük'), findsOneWidget);
  });

  testWidgets('Risk kartı işaret yokken özet sayıları gösterir', (
    tester,
  ) async {
    _useTallScreen(tester);

    await _pump(
      tester,
      ProviderScope(
        overrides: [
          phaseTodayProvider.overrideWith(
            (ref) async => PhaseToday.fromJson(_phaseJson),
          ),
        ],
        child: _app(const HomeScreen()),
      ),
    );

    expect(find.text('Dikkat çeken bir şey yok'), findsOneWidget);
    expect(find.text('6 gün kayıt'), findsOneWidget);
    expect(find.text('5 gün kanama'), findsOneWidget);
    expect(find.text('1 gün lekelenme'), findsOneWidget);
    expect(find.text('2 gün ağrı'), findsOneWidget);
  });

  testWidgets('Risk kartı işaretleri başlık ve açıklamayla listeler', (
    tester,
  ) async {
    _useTallScreen(tester);

    final flagged = Map<String, dynamic>.from(_phaseJson)
      ..['risk'] = {
        ..._riskJson,
        'level': 'Attention',
        'title': 'Dikkat etmeye değer',
        'message': 'Aşağıdakileri bir sağlık profesyoneliyle konuşmayı '
            'düşünebilirsin. Bu bir teşhis değil.',
        'flags': [
          {
            'code': 'ProlongedBleeding',
            'level': 'Attention',
            'title': 'Uzun süren kanama',
            'detail': 'Bu döngüde 8 gün üst üste kanama kaydettin. Bir '
                'haftayı aşan kanamalar takip etmeye değer.',
          },
          {
            'code': 'FrequentPain',
            'level': 'Info',
            'title': 'Sık ağrı kaydı',
            'detail': 'Bu döngüde 6 gün ağrı (kramp, bel veya pelvis) '
                'kaydettin.',
          },
        ],
      };

    await _pump(
      tester,
      ProviderScope(
        overrides: [
          phaseTodayProvider.overrideWith(
            (ref) async => PhaseToday.fromJson(flagged),
          ),
        ],
        child: _app(const HomeScreen()),
      ),
    );

    expect(find.text('Dikkat etmeye değer'), findsOneWidget);
    expect(find.text('Uzun süren kanama'), findsOneWidget);
    expect(find.textContaining('8 gün üst üste kanama'), findsOneWidget);
    expect(find.text('Sık ağrı kaydı'), findsOneWidget);
    expect(find.textContaining('teşhis değil'), findsOneWidget);
  });

  testWidgets('Beslenme ekranı önerilen ve sınırlı listelerini gösterir', (
    tester,
  ) async {
    _useTallScreen(tester);

    await _pump(
      tester,
      ProviderScope(
        overrides: [
          nutritionProvider.overrideWith(
            (ref) async => PhaseContent.fromJson(_nutritionJson),
          ),
        ],
        child: _app(const NutritionScreen()),
      ),
    );

    expect(find.text('Bu fazda öncelik verebileceklerin'), findsOneWidget);
    expect(find.text('Yumurta'), findsOneWidget);
    expect(find.text('Avokado'), findsOneWidget);
    expect(find.text('Sınırlı tutabileceklerin'), findsOneWidget);
    expect(find.text('İşlenmiş şeker'), findsOneWidget);
    expect(find.textContaining('tıbbi tavsiye değildir'), findsOneWidget);

    // Geri bildirim yalnızca önerilerde sorulur, uyarı listesinde değil.
    expect(find.byIcon(Icons.thumb_up_outlined), findsNWidgets(2));
    expect(find.byIcon(Icons.thumb_down_outlined), findsNWidgets(2));
  });

  testWidgets('Hareket ekranı içeriği gösterir', (tester) async {
    _useTallScreen(tester);

    await _pump(
      tester,
      ProviderScope(
        overrides: [
          exerciseProvider.overrideWith(
            (ref) async => PhaseContent.fromJson(_exerciseJson),
          ),
        ],
        child: _app(const ExerciseScreen()),
      ),
    );

    expect(find.text('Kardiyo'), findsOneWidget);
    expect(find.text('Şimdilik erteleyebileceklerin'), findsOneWidget);

    // Süre kısıtı sert filtreye gider; ekranda seçilebilir olmalı.
    expect(find.text('Ne kadar vaktin var?'), findsOneWidget);
    expect(find.text('Fark etmez'), findsOneWidget);
    expect(find.text('10 dk'), findsOneWidget);
    expect(find.text('15 dk'), findsOneWidget);
    expect(find.text('45 dk'), findsOneWidget);

    // Süre rozeti ve "tamamladım" yalnızca hareket önerilerinde var.
    expect(find.text('30 dk'), findsNWidgets(2));
    expect(find.text('Tamamladım'), findsOneWidget);
  });

  testWidgets('Profil ekranı kullanıcı bilgilerini gösterir', (tester) async {
    _useTallScreen(tester);

    await _pump(
      tester,
      ProviderScope(
        overrides: [
          profileProvider.overrideWith(
            (ref) async => UserSummary.fromJson(_userJson),
          ),
        ],
        child: _app(const ProfileScreen()),
      ),
    );

    expect(find.text('Test Kullanıcı'), findsOneWidget);
    expect(find.text('test@serenecycle.app'), findsOneWidget);
    expect(find.text('TK'), findsOneWidget); // avatar baş harfleri
    expect(find.text('28 gün'), findsOneWidget);
    expect(find.text('5 gün'), findsOneWidget);
    expect(find.text('PROFİL BİLGİLERİNİ DÜZENLE'), findsOneWidget);
    expect(find.text('Adet döngünü düzenle'), findsOneWidget);
    expect(find.text('Çıkış Yap'), findsOneWidget);
  });

  testWidgets('Takvim ekranı ayın günlerini ve göstergeleri listeler', (
    tester,
  ) async {
    _useTallScreen(tester);

    await _pump(
      tester,
      ProviderScope(
        overrides: [
          calendarMonthProvider(_currentMonth).overrideWith(
            (ref) async => CalendarMonth.fromJson(_calendarJson(_currentMonth)),
          ),
        ],
        child: _app(const CalendarScreen()),
      ),
    );

    expect(find.text('Takvim'), findsOneWidget);
    expect(find.text('Pzt'), findsOneWidget);
    // Ayın son günü de çiziliyor mu?
    final lastDay = DateTime(
      _currentMonth.year,
      _currentMonth.month + 1,
      0,
    ).day;
    expect(find.text('$lastDay'), findsOneWidget);

    expect(find.text('Lekelenme'), findsOneWidget);

    // Kanama girilen üç gün, kaydedilen kan rengiyle boyanmalı. Gösterge
    // kartındaki damla varsayılan tonda olduğu için sayıma girmiyor.
    final brownDrops = tester
        .widgetList<Icon>(find.byIcon(Icons.water_drop))
        .where((icon) => icon.color == BloodColorOption.brown.swatch);
    expect(brownDrops, hasLength(3));
  });

  testWidgets('Adet kaydı ekranı mevcut kaydı yükler', (tester) async {
    _useTallScreen(tester);

    final date = DateTime(2026, 8, 10);

    await _pump(
      tester,
      ProviderScope(
        overrides: [
          dailyLogProvider(date).overrideWith(
            (ref) async => DailyLogEntry.fromJson(_dailyLogJson),
          ),
          symptomOptionsProvider.overrideWith(
            (ref) async =>
                _symptomsJson.map(SymptomOption.fromJson).toList(),
          ),
        ],
        child: _app(PeriodLogScreen(date: date)),
      ),
    );

    expect(find.text('10 Ağustos 2026'), findsOneWidget);
    expect(find.text('Kanama var'), findsOneWidget);

    // Kanama açık olduğu için düzey ve renk seçenekleri görünür.
    expect(find.text('Az'), findsOneWidget);
    expect(find.text('Orta'), findsOneWidget);
    expect(find.text('Çok'), findsOneWidget);
    expect(find.text('Kırmızı'), findsOneWidget);

    expect(find.text('Lekelenme var'), findsOneWidget);
    expect(find.text('Karın krampları'), findsOneWidget);
  });

  testWidgets('Beslenme ve hareket başlıklarında ikon kalmadı', (
    tester,
  ) async {
    _useTallScreen(tester);

    await _pump(
      tester,
      ProviderScope(
        overrides: [
          nutritionProvider.overrideWith(
            (ref) async => PhaseContent.fromJson(_nutritionJson),
          ),
        ],
        child: _app(const NutritionScreen()),
      ),
    );

    expect(
      tester.widget<AppBar>(find.byType(AppBar)).actions,
      anyOf(isNull, isEmpty),
    );
  });

  testWidgets('Döngü ayarları ekranı mevcut değerlerle açılır', (
    tester,
  ) async {
    _useTallScreen(tester);

    await _pump(
      tester,
      ProviderScope(
        overrides: [
          profileProvider.overrideWith(
            (ref) async => UserSummary.fromJson(_userJson),
          ),
          phaseTodayProvider.overrideWith(
            (ref) async => PhaseToday.fromJson(_phaseJson),
          ),
        ],
        child: _app(const CycleSettingsScreen()),
      ),
    );

    expect(find.text('28 gün'), findsOneWidget);
    expect(find.text('5 gün'), findsOneWidget);
    // Son adet başlangıcı faz yanıtındaki cycleStartDate'ten geliyor.
    expect(find.text('7 Ağustos 2026'), findsOneWidget);

    // Hiçbir şey değişmediyse kaydetmeye gerek yok.
    expect(
      tester
          .widget<FilledButton>(find.widgetWithText(FilledButton, 'Kaydet'))
          .onPressed,
      isNull,
    );
  });

  testWidgets('Döngü uzunluğu artırılınca kaydet aktifleşir', (tester) async {
    _useTallScreen(tester);

    await _pump(
      tester,
      ProviderScope(
        overrides: [
          profileProvider.overrideWith(
            (ref) async => UserSummary.fromJson(_userJson),
          ),
          phaseTodayProvider.overrideWith(
            (ref) async => PhaseToday.fromJson(_phaseJson),
          ),
        ],
        child: _app(const CycleSettingsScreen()),
      ),
    );

    // İlk "Artır" düğmesi döngü uzunluğuna ait.
    await tester.tap(find.byTooltip('Artır').first);
    await tester.pumpAndSettle();

    expect(find.text('29 gün'), findsOneWidget);
    expect(
      tester
          .widget<FilledButton>(find.widgetWithText(FilledButton, 'Kaydet'))
          .onPressed,
      isNotNull,
    );
  });

  testWidgets('API hatası kullanıcıya gösterilir ve tekrar denenebilir', (
    tester,
  ) async {
    await _pump(
      tester,
      ProviderScope(
        overrides: [
          nutritionProvider.overrideWith(
            (ref) async => throw Exception('Sunucuya ulaşılamadı.'),
          ),
        ],
        child: _app(const NutritionScreen()),
      ),
    );

    expect(find.textContaining('Sunucuya ulaşılamadı'), findsOneWidget);
    expect(find.text('Tekrar dene'), findsOneWidget);
  });

  testWidgets('Beğenmedim işareti listeyi anında yeniden yükler', (
    tester,
  ) async {
    _useTallScreen(tester);

    final api = _FeedbackOnlyApi();
    var loadCount = 0;

    await _pump(
      tester,
      ProviderScope(
        overrides: [
          sereneApiProvider.overrideWithValue(api),
          nutritionProvider.overrideWith((ref) async {
            loadCount++;
            return PhaseContent.fromJson(
              loadCount == 1 ? _nutritionJson : _nutritionAfterFeedbackJson,
            );
          }),
        ],
        child: _app(const NutritionScreen()),
      ),
    );

    expect(loadCount, 1);
    expect(find.text('Yumurta'), findsOneWidget);

    await tester.tap(find.byIcon(Icons.thumb_down_outlined).first);
    await tester.pumpAndSettle();

    // Sinyal sunucuya gitti...
    expect(api.feedbackForItems, [9]);

    // ...ve liste yeniden istendi: kullanıcı düğmenin bir şeyi
    // değiştirdiğini görüyor.
    expect(loadCount, 2);
    expect(find.text('Yumurta'), findsNothing);
    expect(find.text('Kinoa'), findsOneWidget);
  });

  testWidgets('Süre filtresi liste yenilenirken ekranda kalır', (tester) async {
    _useTallScreen(tester);

    await _pump(
      tester,
      ProviderScope(
        overrides: [
          exerciseProvider.overrideWith((ref) async {
            final minutes = ref.watch(exerciseMinutesProvider);
            return PhaseContent.fromJson(
              minutes == null ? _exerciseJson : _emptyExerciseJson,
            );
          }),
        ],
        child: _app(const ExerciseScreen()),
      ),
    );

    expect(find.text('Kardiyo'), findsOneWidget);

    await tester.tap(find.widgetWithText(ChoiceChip, '15 dk'));
    await tester.pumpAndSettle();

    // Çipler listenin dışında olduğu için yeniden yüklemeden sağ çıkar.
    expect(find.text('Ne kadar vaktin var?'), findsOneWidget);
    expect(
      tester
          .widget<ChoiceChip>(find.widgetWithText(ChoiceChip, '15 dk'))
          .selected,
      isTrue,
    );

    // Boş liste artık "içerik eklenmedi" demiyor: sebebi süre kısıtı.
    expect(find.textContaining('15 dakikaya sığan'), findsOneWidget);
  });

  testWidgets('Gizlilik ekranı dışa aktarımın özetini gösterir', (
    tester,
  ) async {
    _useTallScreen(tester);

    final api = _ExportOnlyApi();

    await _pump(
      tester,
      ProviderScope(
        overrides: [sereneApiProvider.overrideWithValue(api)],
        child: _app(const PrivacyScreen()),
      ),
    );

    // Veri istenmeden çekilmiyor: dışa aktarım kullanıcının bilinçli bir
    // eylemi olmalı.
    expect(api.exportCount, 0);

    await tester.tap(find.widgetWithText(FilledButton, 'Verilerimi hazırla'));
    await tester.pumpAndSettle();

    expect(api.exportCount, 1);
    expect(find.text('Döngü kaydı'), findsOneWidget);
    expect(find.text('2'), findsOneWidget);
    expect(find.text('Gün kaydı'), findsOneWidget);
    expect(find.text('3'), findsOneWidget);
    expect(find.widgetWithText(FilledButton, 'Panoya kopyala'), findsOneWidget);
  });

  testWidgets('Hesap silme parola sorulmadan gerçekleşmez', (tester) async {
    _useTallScreen(tester);

    final api = _ExportOnlyApi();

    await _pump(
      tester,
      ProviderScope(
        overrides: [sereneApiProvider.overrideWithValue(api)],
        child: _app(const PrivacyScreen()),
      ),
    );

    await tester.tap(find.text('Hesabımı kalıcı olarak sil'));
    await tester.pumpAndSettle();

    // Onay pop-up'ı: boş parolayla gönderilirse istek hiç çıkmamalı.
    expect(find.text('Hesabını sil'), findsOneWidget);

    await tester.tap(find.widgetWithText(FilledButton, 'Kalıcı olarak sil'));
    await tester.pumpAndSettle();

    expect(find.text('Parola gerekli'), findsOneWidget);
    expect(api.deletedWithPassword, isNull);
  });
}

/// Gizlilik ekranının dokunduğu iki ucu taklit eder.
class _ExportOnlyApi implements SereneApi {
  int exportCount = 0;
  String? deletedWithPassword;

  @override
  Future<UserDataExport> exportMyData() async {
    exportCount++;
    return UserDataExport.fromJson(_exportJson);
  }

  @override
  Future<void> deleteAccount(String currentPassword) async {
    deletedWithPassword = currentPassword;
  }

  @override
  dynamic noSuchMethod(Invocation invocation) => throw UnsupportedError(
        'Test bu çağrıyı beklemiyor: ${invocation.memberName}',
      );
}

final _exportJson = <String, dynamic>{
  'format': 'serene-cycle-export-v1',
  'exportedAt': '2026-08-21T09:00:00Z',
  'profile': _userJson,
  'cycles': [
    {'startDate': '2026-07-10', 'endDate': '2026-08-07', 'lengthInDays': 28},
    {'startDate': '2026-08-07', 'endDate': null, 'lengthInDays': null},
  ],
  'dailyLogs': [
    {'date': '2026-08-07', 'hasBleeding': true, 'symptoms': <String>[]},
    {'date': '2026-08-08', 'hasBleeding': true, 'symptoms': <String>[]},
    {'date': '2026-08-09', 'hasBleeding': false, 'symptoms': <String>[]},
  ],
  'preferences': {
    'avoidFlags': ['Vegan'],
    'learnedTastes': <Map<String, dynamic>>[],
  },
  'notice': 'Bu dosya bütün kişisel verilerini içerir.',
};
