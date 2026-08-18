import 'package:flutter/material.dart';

import '../../../core/api/models.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/widgets/soft_shadow_card.dart';

/// Beslenme ve Hareket ekranları aynı iskeleti paylaşır: "öncelik ver"
/// kartı, "sınırlı tut" kartı ve altta tıbbi uyarı.
class PhaseContentView extends StatelessWidget {
  const PhaseContentView({
    required this.content,
    required this.recommendedTitle,
    required this.limitedTitle,
    required this.recommendedIcon,
    required this.limitedIcon,
    this.footer,
    super.key,
  });

  final PhaseContent content;
  final String recommendedTitle;
  final String limitedTitle;
  final IconData recommendedIcon;
  final IconData limitedIcon;
  final Widget? footer;

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.fromLTRB(24, 16, 24, 24),
      children: [
        Text(
          content.phaseName,
          style: const TextStyle(
            fontSize: 14,
            fontWeight: FontWeight.w600,
            letterSpacing: 1.2,
            color: AppColors.secondary,
          ),
        ),
        const SizedBox(height: 16),
        _ContentCard(
          title: recommendedTitle,
          items: content.recommended,
          icon: recommendedIcon,
          iconBackground: AppColors.secondaryContainer,
          iconColor: AppColors.primary,
        ),
        const SizedBox(height: 20),
        _ContentCard(
          title: limitedTitle,
          items: content.limited,
          icon: limitedIcon,
          iconBackground: AppColors.surfaceContainerHigh,
          iconColor: AppColors.tertiary,
        ),
        if (footer != null) ...[
          const SizedBox(height: 24),
          footer!,
        ],
        const SizedBox(height: 20),
        Text(
          content.disclaimer,
          textAlign: TextAlign.center,
          style: const TextStyle(fontSize: 12, color: AppColors.outline),
        ),
      ],
    );
  }
}

class _ContentCard extends StatelessWidget {
  const _ContentCard({
    required this.title,
    required this.items,
    required this.icon,
    required this.iconBackground,
    required this.iconColor,
  });

  final String title;
  final List<ContentItem> items;
  final IconData icon;
  final Color iconBackground;
  final Color iconColor;

  @override
  Widget build(BuildContext context) {
    return SoftShadowCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            title,
            style: const TextStyle(
              fontSize: 20,
              height: 28 / 20,
              fontWeight: FontWeight.w700,
              color: AppColors.primary,
            ),
          ),
          const SizedBox(height: 12),
          if (items.isEmpty)
            const Padding(
              padding: EdgeInsets.symmetric(vertical: 8),
              child: Text(
                'Bu faz için henüz içerik eklenmedi.',
                style: TextStyle(
                  fontSize: 14,
                  color: AppColors.onSurfaceVariant,
                ),
              ),
            )
          else
            ...List.generate(items.length, (index) {
              final item = items[index];
              final isLast = index == items.length - 1;

              return Padding(
                padding: EdgeInsets.only(bottom: isLast ? 0 : 16),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Container(
                      width: 40,
                      height: 40,
                      decoration: BoxDecoration(
                        color: iconBackground,
                        shape: BoxShape.circle,
                      ),
                      child: Icon(icon, size: 20, color: iconColor),
                    ),
                    const SizedBox(width: 16),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            item.title,
                            style: const TextStyle(
                              fontSize: 14,
                              fontWeight: FontWeight.w600,
                              color: AppColors.onSurface,
                            ),
                          ),
                          const SizedBox(height: 2),
                          Text(
                            item.body,
                            style: const TextStyle(
                              fontSize: 14,
                              height: 20 / 14,
                              color: AppColors.onSurfaceVariant,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              );
            }),
        ],
      ),
    );
  }
}
