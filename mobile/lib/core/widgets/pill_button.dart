import 'package:flutter/material.dart';

import '../theme/app_colors.dart';

/// Tasarımdaki tam genişlikte, hap şeklinde birincil aksiyon butonu.
/// [filled] true ise koyu `primary` zemin, false ise `primary-container`.
class PillButton extends StatelessWidget {
  const PillButton({
    required this.label,
    required this.onPressed,
    this.filled = false,
    this.isLoading = false,
    this.trailingIcon,
    super.key,
  });

  final String label;
  final VoidCallback? onPressed;
  final bool filled;
  final bool isLoading;
  final IconData? trailingIcon;

  static const _trailingIconSize = 18.0;
  static const _spinnerSize = 20.0;
  static const _spinnerStrokeWidth = 2.0;
  static const _disabledOpacity = 0.6;

  @override
  Widget build(BuildContext context) {
    final background =
        filled ? AppColors.primary : AppColors.primaryContainer;
    final foreground =
        filled ? AppColors.onPrimary : AppColors.onPrimaryContainer;

    return SizedBox(
      width: double.infinity,
      child: FilledButton(
        onPressed: isLoading ? null : onPressed,
        style: FilledButton.styleFrom(
          backgroundColor: background,
          foregroundColor: foreground,
          disabledBackgroundColor:
              background.withValues(alpha: _disabledOpacity),
          shape: const StadiumBorder(),
          padding: const EdgeInsets.symmetric(vertical: 16),
        ),
        child: isLoading
            ? SizedBox(
                height: _spinnerSize,
                width: _spinnerSize,
                child: CircularProgressIndicator(
                  strokeWidth: _spinnerStrokeWidth,
                  color: foreground,
                ),
              )
            : Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  // Yazı tipi ve rengi FilledButton'ın kendi metin stilinden
                  // (tema `labelLarge` + `foregroundColor`) geliyor.
                  Text(label),
                  if (trailingIcon != null) ...[
                    const SizedBox(width: 8),
                    Icon(trailingIcon, size: _trailingIconSize),
                  ],
                ],
              ),
      ),
    );
  }
}
