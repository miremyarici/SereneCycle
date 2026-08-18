import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/providers/app_providers.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/widgets/async_view.dart';
import '../../content/presentation/phase_content_view.dart';

class NutritionScreen extends ConsumerWidget {
  const NutritionScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final nutrition = ref.watch(nutritionProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text(
          'Beslenme',
          style: TextStyle(
            fontSize: 20,
            fontWeight: FontWeight.w600,
            color: AppColors.primary,
          ),
        ),
        centerTitle: true,
      ),
      body: AsyncView(
        value: nutrition,
        onRetry: () => ref.invalidate(nutritionProvider),
        builder: (data) => RefreshIndicator(
          onRefresh: () async => ref.refresh(nutritionProvider.future),
          child: PhaseContentView(
            content: data,
            recommendedTitle: 'Bu fazda öncelik verebileceklerin',
            limitedTitle: 'Sınırlı tutabileceklerin',
            recommendedIcon: Icons.restaurant_outlined,
            limitedIcon: Icons.remove_circle_outline,
            // Faz 4'te YOLO tarama ekranına bağlanacak.
            footer: FilledButton.icon(
              onPressed: () => ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(
                  content: Text('Yiyecek tarama Faz 4\'te geliyor.'),
                ),
              ),
              icon: const Icon(Icons.photo_camera_outlined),
              label: const Text('YİYECEK TARA'),
              style: FilledButton.styleFrom(
                backgroundColor: AppColors.primary,
                foregroundColor: AppColors.onPrimary,
                shape: const StadiumBorder(),
                padding: const EdgeInsets.symmetric(vertical: 16),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
