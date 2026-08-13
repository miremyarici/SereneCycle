import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:serene_cycle/app.dart';

void main() {
  testWidgets('4 sekmeli alt menü açılışta Ana Sayfa\'yı gösterir', (
    tester,
  ) async {
    await tester.pumpWidget(
      const ProviderScope(child: SereneCycleApp()),
    );
    await tester.pumpAndSettle();

    expect(find.text('Ana Sayfa — Faz 1'), findsOneWidget);
    expect(find.text('Ana Sayfa'), findsOneWidget);
    expect(find.text('Beslenme'), findsOneWidget);
    expect(find.text('Hareket'), findsOneWidget);
    expect(find.text('Profil'), findsOneWidget);
  });

  testWidgets('Beslenme sekmesine geçiş ekranı değiştirir', (tester) async {
    await tester.pumpWidget(
      const ProviderScope(child: SereneCycleApp()),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.text('Beslenme'));
    await tester.pumpAndSettle();

    expect(find.text('Beslenme Desteği — Faz 2'), findsOneWidget);
  });
}
