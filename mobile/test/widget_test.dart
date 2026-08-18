import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:serene_cycle/app.dart';

Future<void> _pumpApp(WidgetTester tester) async {
  await tester.pumpWidget(const ProviderScope(child: SereneCycleApp()));
  await tester.pumpAndSettle();
}

void main() {
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
}
