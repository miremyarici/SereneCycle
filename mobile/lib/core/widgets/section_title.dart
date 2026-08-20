import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_typography.dart';

/// Kart içindeki büyük harfli bölüm başlığı ("DÖNGÜ AYARLARI", "BU HAFTA").
/// Dört ekranda birebir aynı stil kopyalanmıştı; tek yerden geliyor.
class SectionTitle extends StatelessWidget {
  const SectionTitle(this.text, {this.color = AppColors.secondary, super.key});

  final String text;
  final Color color;

  @override
  Widget build(BuildContext context) =>
      Text(text, style: AppTypography.sectionHeader.copyWith(color: color));
}
