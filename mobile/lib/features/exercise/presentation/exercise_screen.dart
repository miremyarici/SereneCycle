import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/providers/app_providers.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_typography.dart';
import '../../../core/widgets/async_view.dart';
import '../../content/presentation/phase_content_view.dart';

class ExerciseScreen extends ConsumerWidget {
  const ExerciseScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final exercise = ref.watch(exerciseProvider);
    final minutes = ref.watch(exerciseMinutesProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Hareket'),
        centerTitle: true,
      ),
      // Süre filtresi listenin dışında duruyor: içeride olsaydı her seçimde
      // liste yeniden yüklenirken çipler de ekrandan kaybolurdu.
      body: Column(
        children: [
          const Padding(
            padding: EdgeInsets.fromLTRB(24, 8, 24, 0),
            child: _DurationFilter(),
          ),
          Expanded(
            child: AsyncView(
              value: exercise,
              onRetry: () => ref.invalidate(exerciseProvider),
              builder: (data) => RefreshIndicator(
                onRefresh: () async => ref.refresh(exerciseProvider.future),
                child: PhaseContentView(
                  content: data,
                  surface: ContentSurface.exercise,
                  recommendedTitle: 'Bu fazda iyi gelebilecek hareketler',
                  // "Yasak" değil: karar kullanıcının, gerekçesi yanında.
                  limitedTitle: 'Şimdilik erteleyebileceklerin',
                  recommendedIcon: Icons.self_improvement_outlined,
                  limitedIcon: Icons.pause_circle_outline,
                  recommendedEmptyMessage: minutes == null
                      ? null
                      : 'Bu faz için $minutes dakikaya sığan ve '
                          'kısıtlarına uyan bir hareket bulunamadı. Daha uzun '
                          'bir süre seçmeyi deneyebilirsin.',
                  showCompletedAction: true,
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

/// Süre bir tercih değil, o anki kısıt: seçilen süreden uzun hareketler
/// listeye hiç girmez.
class _DurationFilter extends ConsumerWidget {
  const _DurationFilter();

  /// null = "süre kısıtım yok". Katalogda her fazda bu aralıkların hepsine
  /// oturan hareket var; testler bunu garanti ediyor.
  static const _options = <int?>[null, 10, 15, 30, 45];

  static String _labelOf(int? minutes) =>
      minutes == null ? 'Fark etmez' : '$minutes dk';

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final selected = ref.watch(exerciseMinutesProvider);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          'Ne kadar vaktin var?',
          style: context.text.labelLarge
              ?.copyWith(color: AppColors.onSurface),
        ),
        const SizedBox(height: 8),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: [
            for (final minutes in _options)
              ChoiceChip(
                label: Text(_labelOf(minutes)),
                selected: selected == minutes,
                onSelected: (_) => ref
                    .read(exerciseMinutesProvider.notifier)
                    .select(minutes),
              ),
          ],
        ),
      ],
    );
  }
}
