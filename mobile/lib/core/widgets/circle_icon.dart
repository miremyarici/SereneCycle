import 'package:flutter/material.dart';

import '../theme/app_colors.dart';

/// Ayar/aksiyon satırlarının solundaki yuvarlak ikon rozeti. Profil, hesap
/// ve döngü ayarları ekranlarında aynı 40x40 daire tekrar ediyordu.
class CircleIcon extends StatelessWidget {
  const CircleIcon(
    this.icon, {
    this.background = AppColors.surfaceContainer,
    this.foreground = AppColors.secondary,
    this.diameter = 40,
    this.iconSize = 20,
    super.key,
  });

  final IconData icon;
  final Color background;
  final Color foreground;
  final double diameter;
  final double iconSize;

  @override
  Widget build(BuildContext context) => Container(
        width: diameter,
        height: diameter,
        decoration: BoxDecoration(color: background, shape: BoxShape.circle),
        child: Icon(icon, size: iconSize, color: foreground),
      );
}
