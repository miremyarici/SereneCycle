import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../../core/api/models.dart';
import '../../../core/providers/app_providers.dart';
import '../../../core/router/route_paths.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_typography.dart';
import '../../../core/widgets/async_view.dart';
import '../../../core/widgets/section_title.dart';
import '../../../core/widgets/soft_shadow_card.dart';
import 'widgets/cycle_progress_ring.dart';
import 'widgets/risk_card.dart';
import 'widgets/week_calendar_strip.dart';

class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  static const _cardSpacing = 20.0;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final phase = ref.watch(phaseTodayProvider);

    return Scaffold(
      appBar: AppBar(
        title: Text('Serene Cycle', style: context.text.headlineLarge),
      ),
      body: AsyncView(
        value: phase,
        onRetry: () => ref.invalidate(phaseTodayProvider),
        builder: (data) => RefreshIndicator(
          onRefresh: () async => ref.refresh(phaseTodayProvider.future),
          child: ListView(
            padding: const EdgeInsets.fromLTRB(24, 8, 24, 24),
            children: [
              _PhaseCard(phase: data),
              const SizedBox(height: _cardSpacing),
              RiskCard(risk: data.risk),
              const SizedBox(height: _cardSpacing),
              _CalendarCard(days: data.calendarStrip),
              const SizedBox(height: _cardSpacing),
              _MoodsCard(moods: data.commonMoods),
              const SizedBox(height: _cardSpacing),
              const _Disclaimer(),
            ],
          ),
        ),
      ),
    );
  }
}

class _PhaseCard extends StatelessWidget {
  const _PhaseCard({required this.phase});

  final PhaseToday phase;

  @override
  Widget build(BuildContext context) {
    final dateFormat = DateFormat('d MMMM', 'tr');

    return SoftShadowCard(
      child: Column(
        children: [
          Text(
            phase.phaseName,
            style:
                context.text.headlineSmall?.copyWith(color: AppColors.primary),
          ),
          const SizedBox(height: 20),
          CycleProgressRing(
            cycleDay: phase.cycleDay,
            cycleLength: phase.cycleLength,
            progress: phase.progress,
          ),
          const SizedBox(height: 20),
          Text(
            phase.phaseDescription,
            textAlign: TextAlign.center,
            style: context.text.bodyLarge
                ?.copyWith(color: AppColors.onSurfaceVariant),
          ),
          const SizedBox(height: 20),
          Row(
            children: [
              Expanded(
                child: _Prediction(
                  label: 'Tahmini adet',
                  value: dateFormat.format(phase.predictedNextPeriod),
                  icon: Icons.water_drop_outlined,
                ),
              ),
              Expanded(
                child: _Prediction(
                  label: 'Tahmini ovulasyon',
                  value: dateFormat.format(phase.predictedOvulation),
                  icon: Icons.egg_outlined,
                ),
              ),
            ],
          ),
          // Tahminlerin güveni düşükse bunu saklamıyoruz, açıkça söylüyoruz.
          if (phase.isIrregular) ...[
            const SizedBox(height: 16),
            const _Notice(
              icon: Icons.info_outline,
              text: 'Döngü uzunluğun olağan aralığın dışında, '
                  'bu yüzden tahminlerin güveni düşük.',
            ),
          ],
          if (phase.isPeriodLate) ...[
            const SizedBox(height: 12),
            const _Notice(
              icon: Icons.schedule,
              text: 'Beklenen adet tarihi geçti. Adetin başladıysa '
                  'kaydetmen tahminleri düzeltir.',
            ),
          ],
        ],
      ),
    );
  }
}

class _Prediction extends StatelessWidget {
  const _Prediction({
    required this.label,
    required this.value,
    required this.icon,
  });

  final String label;
  final String value;
  final IconData icon;

  @override
  Widget build(BuildContext context) => Column(
        children: [
          Icon(icon, size: 20, color: AppColors.secondary),
          const SizedBox(height: 6),
          Text(
            label,
            style: context.text.bodySmall
                ?.copyWith(color: AppColors.onSurfaceVariant),
          ),
          const SizedBox(height: 2),
          Text(
            value,
            style:
                context.text.labelLarge?.copyWith(color: AppColors.onSurface),
          ),
        ],
      );
}

class _Notice extends StatelessWidget {
  const _Notice({required this.icon, required this.text});

  final IconData icon;
  final String text;

  @override
  Widget build(BuildContext context) => Container(
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          color: AppColors.surfaceContainerLow,
          borderRadius: BorderRadius.circular(12),
        ),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(icon, size: 18, color: AppColors.secondary),
            const SizedBox(width: 8),
            Expanded(
              child: Text(
                text,
                style: context.text.bodyMedium
                    ?.copyWith(color: AppColors.onSurfaceVariant),
              ),
            ),
          ],
        ),
      );
}

class _CalendarCard extends StatelessWidget {
  const _CalendarCard({required this.days});

  final List<CalendarDay> days;

  @override
  Widget build(BuildContext context) => SoftShadowCard(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                const SectionTitle('BU HAFTA', color: AppColors.primary),
                IconButton(
                  onPressed: () => context.push(RoutePaths.calendar),
                  tooltip: 'Tüm takvimi aç',
                  visualDensity: VisualDensity.compact,
                  icon: const Icon(
                    Icons.calendar_month_outlined,
                    size: 20,
                    color: AppColors.onSurfaceVariant,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),
            // Şeritteki bir güne dokunmak da aynı kayıt ekranını açar.
            WeekCalendarStrip(
              days: days,
              onDayTap: (day) => context.push(RoutePaths.periodLog(day.date)),
            ),
            const SizedBox(height: 12),
            const Row(
              children: [
                _Legend(color: AppColors.primary, label: 'Adet günü'),
                SizedBox(width: 16),
                _Legend(
                  color: AppColors.secondaryContainer,
                  label: 'Kayıt var',
                ),
              ],
            ),
          ],
        ),
      );
}

class _Legend extends StatelessWidget {
  const _Legend({required this.color, required this.label});

  static const _dotSize = 6.0;

  final Color color;
  final String label;

  @override
  Widget build(BuildContext context) => Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            width: _dotSize,
            height: _dotSize,
            decoration: BoxDecoration(color: color, shape: BoxShape.circle),
          ),
          const SizedBox(width: 6),
          Text(
            label,
            style: context.text.bodySmall
                ?.copyWith(color: AppColors.onSurfaceVariant),
          ),
        ],
      );
}

class _MoodsCard extends StatelessWidget {
  const _MoodsCard({required this.moods});

  final List<String> moods;

  @override
  Widget build(BuildContext context) => SoftShadowCard(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Bu fazda yaygın duygular',
              style: context.text.headlineSmall
                  ?.copyWith(color: AppColors.primary),
            ),
            const SizedBox(height: 16),
            Wrap(
              spacing: 12,
              runSpacing: 12,
              children: [
                for (final mood in moods) _MoodChip(mood),
              ],
            ),
          ],
        ),
      );
}

class _MoodChip extends StatelessWidget {
  const _MoodChip(this.mood);

  final String mood;

  @override
  Widget build(BuildContext context) => Container(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
        decoration: BoxDecoration(
          border: Border.all(color: AppColors.outlineVariant),
          borderRadius: BorderRadius.circular(999),
        ),
        child: Text(
          mood,
          style: context.text.labelLarge?.copyWith(color: AppColors.onSurface),
        ),
      );
}

class _Disclaimer extends StatelessWidget {
  const _Disclaimer();

  @override
  Widget build(BuildContext context) => Text(
        'Bu içerik bilgilendirme amaçlıdır, tıbbi tavsiye değildir.',
        textAlign: TextAlign.center,
        style: context.text.bodySmall?.copyWith(color: AppColors.outline),
      );
}
