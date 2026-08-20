import 'package:flutter/material.dart';

import '../theme/app_colors.dart';

/// Sayı artır/azalt düğmesi. Onboarding sihirbazı büyük ([isLarge]),
/// döngü ayarları satır içi küçük varyantı kullanıyor.
class StepperButton extends StatelessWidget {
  const StepperButton({
    required this.icon,
    required this.tooltip,
    required this.onPressed,
    this.isLarge = false,
    super.key,
  });

  static const _largeSize = Size(56, 56);
  static const _largeIconSize = 24.0;
  static const _compactIconSize = 20.0;

  final IconData icon;
  final String tooltip;
  final VoidCallback? onPressed;
  final bool isLarge;

  @override
  Widget build(BuildContext context) => IconButton(
        onPressed: onPressed,
        tooltip: tooltip,
        visualDensity: isLarge ? null : VisualDensity.compact,
        icon: Icon(icon, size: isLarge ? _largeIconSize : _compactIconSize),
        style: IconButton.styleFrom(
          backgroundColor: AppColors.surfaceContainer,
          foregroundColor: AppColors.primary,
          disabledForegroundColor: AppColors.outline,
          minimumSize: isLarge ? _largeSize : null,
          shape: const CircleBorder(),
        ),
      );
}
