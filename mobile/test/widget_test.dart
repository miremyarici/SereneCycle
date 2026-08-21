import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:serene_cycle/app.dart';
import 'package:serene_cycle/core/api/api_client.dart';
import 'package:serene_cycle/core/api/models.dart';
import 'package:serene_cycle/core/api/serene_api.dart';
import 'package:serene_cycle/core/api/token_storage.dart';
import 'package:serene_cycle/core/providers/app_providers.dart';

Future<void> _pumpApp(WidgetTester tester) async {
  await tester.pumpWidget(const ProviderScope(child: SereneCycleApp()));
  await tester.pumpAndSettle();
}

void main() {
  setUp(() {
    // Güvenli depo eklentisi test ortamında yok; paketin bellek içi sahtesi
    // olmadan açılıştaki oturum okuması hiç sonuçlanmıyor.
    FlutterSecureStorage.setMockInitialValues({});
  });

  testWidgets('uygulama giriş ekranından başlar', (tester) async {
    await _pumpApp(tester);

    expect(find.text('Tekrar hoş geldin'), findsOneWidget);
    expect(find.text('Giriş Yap'), findsOneWidget);
  });

  testWidgets('kayıt ol bağlantısı kayıt ekranını açar', (tester) async {
    await _pumpApp(tester);

    // Varsayılan 800x600 test yüzeyinde bağlantı ekranın altında kalıyor.
    await tester.ensureVisible(find.text('Kayıt Ol'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Kayıt Ol'));
    await tester.pumpAndSettle();

    expect(find.text('Yolculuğuna başla'), findsOneWidget);
  });

  testWidgets('boş form gönderilince doğrulama hataları görünür', (
    tester,
  ) async {
    await _pumpApp(tester);

    await tester.tap(find.widgetWithText(FilledButton, 'Giriş Yap'));
    await tester.pumpAndSettle();

    expect(find.text('E-posta gerekli'), findsOneWidget);
    expect(find.text('Şifre gerekli'), findsOneWidget);
  });

  testWidgets('şifremi unuttum bağlantısı ilgili ekranı açar', (tester) async {
    await _pumpApp(tester);

    await tester.ensureVisible(find.text('Şifremi unuttum'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Şifremi unuttum'));
    await tester.pumpAndSettle();

    expect(find.text('Şifreni mi unuttun?'), findsOneWidget);
  });

  // --- Oturum devam ettirme ---
  //
  // Uygulama açılışta güvenli depodaki refresh token'la `/me` deniyor.
  // Kullanıcı her açılışta yeniden giriş yapmak zorunda kalmamalı, ama
  // token gerçekten ölmüşse de giriş ekranında bırakılmalı.

  testWidgets('kayıtlı oturumla açılınca giriş ekranı atlanır', (tester) async {
    await _saveSession();

    await _pumpAppWithApi(tester, _StubApi(_user));

    expect(find.text('Tekrar hoş geldin'), findsNothing);
    // Onboarding'i tamamlamamış kullanıcı doğrudan sihirbaza düşer.
    expect(find.text('Son adet başlangıcın ne zaman?'), findsOneWidget);
  });

  testWidgets('sunucu oturumu reddederse giriş ekranına düşülür', (
    tester,
  ) async {
    await _saveSession();

    await _pumpAppWithApi(
      tester,
      _StubApi.rejecting(),
    );

    expect(find.text('Tekrar hoş geldin'), findsOneWidget);

    // Ölü token bir daha denenmesin diye depodan silinmiş olmalı.
    expect(await TokenStorage().readRefreshToken(), isNull);
  });
}

final _user = UserSummary(
  id: '00000000-0000-0000-0000-000000000001',
  name: 'Test Kullanıcı',
  email: 'test@serenecycle.app',
  emailConfirmed: true,
  avgCycleLength: 28,
  avgPeriodLength: 5,
  hasCompletedOnboarding: false,
);

Future<void> _saveSession() => TokenStorage().save(
      accessToken: 'access-1',
      refreshToken: 'refresh-1',
    );

Future<void> _pumpAppWithApi(WidgetTester tester, SereneApi api) async {
  await tester.pumpWidget(
    ProviderScope(
      overrides: [sereneApiProvider.overrideWithValue(api)],
      child: const SereneCycleApp(),
    ),
  );
  await tester.pumpAndSettle();
}

/// Yalnızca `/me` ucunu taklit eder; açılışta çağrılan tek uç o.
/// Beklenmeyen bir çağrı sessizce geçmesin diye `noSuchMethod` hata atar.
class _StubApi implements SereneApi {
  _StubApi(this._user);

  _StubApi.rejecting() : _user = null;

  final UserSummary? _user;

  @override
  Future<UserSummary> getMe() async {
    final user = _user;

    if (user == null) {
      throw const ApiException('Oturum geçersiz.', statusCode: 401);
    }

    return user;
  }

  @override
  dynamic noSuchMethod(Invocation invocation) => throw UnsupportedError(
        'Test bu çağrıyı beklemiyor: ${invocation.memberName}',
      );
}
