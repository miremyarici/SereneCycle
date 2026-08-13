import 'package:flutter/material.dart';

import '../theme/app_colors.dart';

/// Tasarımdaki standart kart: beyaz zemin, 16px köşe,
/// yumuşak kahverengi gölge (0px 4px 20px rgba(74,52,41,0.06)).
class SoftShadowCard extends StatelessWidget {
  const SoftShadowCard({
    required this.child,
    this.padding = const EdgeInsets.all(20),
    super.key,
  });

  final Widget child;
  final EdgeInsetsGeometry padding;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: padding,
      decoration: BoxDecoration(
        color: AppColors.surfaceContainerLowest,
        borderRadius: BorderRadius.circular(16),
        boxShadow: const [
          BoxShadow(
            color: Color(0x0F4A3429), // rgba(74,52,41,0.06)
            blurRadius: 20,
            offset: Offset(0, 4),
          ),
        ],
      ),
      child: child,
    );
  }
}
