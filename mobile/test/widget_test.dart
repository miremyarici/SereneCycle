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

  testWidgets('giriş yapınca 4 sekmeli shell açılır ve sekmeler değişir', (
    tester,
  ) async {
    await _pumpApp(tester);

    await tester.enterText(
      find.byType(TextFormField).first,
      'test@ornek.com',
    );
    await tester.enterText(find.byType(TextFormField).last, 'sifre1234');
    await tester.tap(find.text('Giriş Yap'));
    await tester.pumpAndSettle();

    expect(find.text('Ana Sayfa — Faz 1'), findsOneWidget);

    // Alt menüde etiket yok; ikonla geçiş yapılıyor.
    await tester.tap(find.byIcon(Icons.restaurant_outlined));
    await tester.pumpAndSettle();

    expect(find.text('Beslenme Desteği — Faz 2'), findsOneWidget);
  });
}
