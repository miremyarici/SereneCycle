import 'package:flutter/material.dart';

import '../../../../core/api/models.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_typography.dart';
import '../../../../core/widgets/soft_shadow_card.dart';

/// Ana sayfadaki risk kartı. Bütün metinler ve eşikler sunucudan geliyor;
/// bu widget yalnızca seviyeye göre ton seçip listeliyor.
class RiskCard extends StatelessWidget {
  const RiskCard({required this.risk, super.key});

  final CycleRisk risk;

  @override
  Widget build(BuildContext context) {
    final tone = _RiskTone.of(risk.level);

    return SoftShadowCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                padding: const EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: tone.background,
                  shape: BoxShape.circle,
                ),
                child: Icon(tone.icon, size: 20, color: tone.accent),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Text(
                  risk.title,
                  style: context.text.headlineSmall
                      ?.copyWith(color: AppColors.primary),
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          Text(
            risk.message,
            style: context.text.bodyMedium
                ?.copyWith(color: AppColors.onSurfaceVariant),
          ),
          for (final flag in risk.flags) ...[
            const SizedBox(height: 12),
            _FlagRow(flag: flag),
          ],
          // Hiç kayıt yoksa gösterecek sayı da yok.
          if (risk.stats.loggedDays > 0) ...[
            const SizedBox(height: 16),
            _StatsStrip(stats: risk.stats),
          ],
        ],
      ),
    );
  }
}

class _FlagRow extends StatelessWidget {
  const _FlagRow({required this.flag});

  final RiskFlag flag;

  @override
  Widget build(BuildContext context) {
    final tone = _RiskTone.of(flag.level);

    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: tone.background,
        borderRadius: BorderRadius.circular(12),
        // Sol kenardaki şerit, seviyeyi renk körlüğünde de ayırt edilebilir
        // kılmak için ikonun yanında ikinci bir işaret.
        border: Border(left: BorderSide(color: tone.accent, width: 3)),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(tone.icon, size: 18, color: tone.accent),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  flag.title,
                  style: context.text.titleSmall
                      ?.copyWith(color: AppColors.onSurface),
                ),
                const SizedBox(height: 2),
                Text(
                  flag.detail,
                  style: context.text.bodyMedium
                      ?.copyWith(color: AppColors.onSurfaceVariant),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _StatsStrip extends StatelessWidget {
  const _StatsStrip({required this.stats});

  final RiskStats stats;

  @override
  Widget build(BuildContext context) => Wrap(
        spacing: 16,
        runSpacing: 8,
        children: [
          _Stat(
            icon: Icons.event_note_outlined,
            label: '${stats.loggedDays} gün kayıt',
          ),
          _Stat(
            icon: Icons.water_drop_outlined,
            label: '${stats.bleedingDays} gün kanama',
            color: AppColors.bleeding,
          ),
          if (stats.spottingDays > 0)
            _Stat(
              icon: Icons.circle,
              label: '${stats.spottingDays} gün lekelenme',
              color: AppColors.spotting,
            ),
          if (stats.painDays > 0)
            _Stat(
              icon: Icons.healing_outlined,
              label: '${stats.painDays} gün ağrı',
            ),
        ],
      );
}

class _Stat extends StatelessWidget {
  const _Stat({required this.icon, required this.label, this.color});

  final IconData icon;
  final String label;
  final Color? color;

  @override
  Widget build(BuildContext context) => Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 14, color: color ?? AppColors.outline),
          const SizedBox(width: 4),
          Text(
            label,
            style: context.text.bodySmall
                ?.copyWith(color: AppColors.onSurfaceVariant),
          ),
        ],
      );
}

/// Seviye → ikon ve renk. Tek yerde tutuluyor ki kart başlığı ile satırlar
/// aynı dili konuşsun.
class _RiskTone {
  const _RiskTone({
    required this.icon,
    required this.accent,
    required this.background,
  });

  factory _RiskTone.of(RiskLevel level) => switch (level) {
        RiskLevel.attention => const _RiskTone(
            icon: Icons.error_outline,
            accent: AppColors.error,
            background: AppColors.errorContainer,
          ),
        RiskLevel.info => const _RiskTone(
            icon: Icons.info_outline,
            accent: AppColors.spotting,
            background: AppColors.surfaceContainerHigh,
          ),
        RiskLevel.none => const _RiskTone(
            icon: Icons.check_circle_outline,
            accent: AppColors.secondary,
            background: AppColors.surfaceContainerLow,
          ),
      };

  final IconData icon;
  final Color accent;
  final Color background;
}
