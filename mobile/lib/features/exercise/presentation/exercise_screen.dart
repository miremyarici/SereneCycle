import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/providers/app_providers.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/widgets/async_view.dart';
import '../../content/presentation/phase_content_view.dart';

class ExerciseScreen extends ConsumerWidget {
  const ExerciseScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final exercise = ref.watch(exerciseProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text(
          'Hareket',
          style: TextStyle(
            fontSize: 20,
            fontWeight: FontWeight.w600,
            color: AppColors.primary,
          ),
        ),
        centerTitle: true,
      ),
      body: AsyncView(
        value: exercise,
        onRetry: () => ref.invalidate(exerciseProvider),
        builder: (data) => RefreshIndicator(
          onRefresh: () async => ref.refresh(exerciseProvider.future),
          child: PhaseContentView(
            content: data,
            recommendedTitle: 'Bu fazda iyi gelebilecek hareketler',
            // "Yasak" değil: karar kullanıcının, gerekçesi yanında.
            limitedTitle: 'Şimdilik erteleyebileceklerin',
            recommendedIcon: Icons.self_improvement_outlined,
            limitedIcon: Icons.pause_circle_outline,
          ),
        ),
      ),
    );
  }
}
