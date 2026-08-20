import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_typography.dart';

/// Auth kartlarının tepesindeki başlık + açıklama ikilisi. Beş ekranda aynı
/// ölçek ve hizalama tekrarlanıyordu.
class AuthHeader extends StatelessWidget {
  const AuthHeader({required this.title, required this.subtitle, super.key});

  final String title;
  final String subtitle;

  @override
  Widget build(BuildContext context) => Column(
        children: [
          Text(
            title,
            textAlign: TextAlign.center,
            style: context.text.headlineMedium
                ?.copyWith(color: AppColors.onSurface),
          ),
          const SizedBox(height: 8),
          Text(
            subtitle,
            textAlign: TextAlign.center,
            style: context.text.bodyLarge
                ?.copyWith(color: AppColors.onSurfaceVariant),
          ),
        ],
      );
}
