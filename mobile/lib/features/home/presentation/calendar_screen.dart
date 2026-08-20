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
import '../../../core/widgets/soft_shadow_card.dart';
import 'widgets/cycle_day_marker.dart';
import 'widgets/weekday_labels.dart';

/// Ana sayfadaki takvim ikonundan açılır: kullanıcı tüm tarihleri görür ve
/// bir güne dokununca o günün adet kaydı ekranına gider.
class CalendarScreen extends ConsumerStatefulWidget {
  const CalendarScreen({super.key});

  @override
  ConsumerState<CalendarScreen> createState() => _CalendarScreenState();
}

class _CalendarScreenState extends ConsumerState<CalendarScreen> {
  /// Ay değişirken kart boyu zıplamasın diye sabit yükseklik: 6 satırlık
  /// en uzun aya göre.
  static const _gridHeight = 380.0;

  late DateTime _month = _firstDayOfMonth(DateTime.now());

  static DateTime _firstDayOfMonth(DateTime date) =>
      DateTime(date.year, date.month);

  void _shiftMonth(int delta) {
    setState(() => _month = DateTime(_month.year, _month.month + delta));
  }

  @override
  Widget build(BuildContext context) {
    final calendar = ref.watch(calendarMonthProvider(_month));

    return Scaffold(
      appBar: AppBar(title: const Text('Takvim'), centerTitle: true),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(24, 8, 24, 32),
        children: [
          _MonthHeader(
            month: _month,
            onPrevious: () => _shiftMonth(-1),
            onNext: () => _shiftMonth(1),
          ),
          const SizedBox(height: 16),
          SoftShadowCard(
            child: SizedBox(
              height: _gridHeight,
              child: AsyncView(
                value: calendar,
                onRetry: () => ref.invalidate(calendarMonthProvider(_month)),
                builder: (data) => _MonthGrid(
                  month: _month,
                  days: data.days,
                  onDayTap: (day) =>
                      context.push(RoutePaths.periodLog(day.date)),
                ),
              ),
            ),
          ),
          const SizedBox(height: 20),
          const _Legend(),
        ],
      ),
    );
  }
}

class _MonthHeader extends StatelessWidget {
  const _MonthHeader({
    required this.month,
    required this.onPrevious,
    required this.onNext,
  });

  final DateTime month;
  final VoidCallback onPrevious;
  final VoidCallback onNext;

  @override
  Widget build(BuildContext context) => Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          IconButton(
            onPressed: onPrevious,
            tooltip: 'Önceki ay',
            icon: const Icon(Icons.chevron_left, color: AppColors.primary),
          ),
          Text(
            DateFormat('MMMM yyyy', 'tr').format(month),
            style: context.text.titleLarge?.copyWith(color: AppColors.primary),
          ),
          IconButton(
            onPressed: onNext,
            tooltip: 'Sonraki ay',
            icon: const Icon(Icons.chevron_right, color: AppColors.primary),
          ),
        ],
      );
}

class _MonthGrid extends StatelessWidget {
  const _MonthGrid({
    required this.month,
    required this.days,
    required this.onDayTap,
  });

  static const _columnCount = 7;
  static const _cellAspectRatio = 0.72;

  final DateTime month;
  final List<CalendarDay> days;
  final void Function(CalendarDay day) onDayTap;

  @override
  Widget build(BuildContext context) {
    // DateTime.weekday: 1 = Pazartesi. Ayın ilk günü haftanın ortasına
    // düşüyorsa öncesini boş hücrelerle dolduruyoruz.
    final leadingBlanks = DateTime(month.year, month.month).weekday - 1;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Row(
          children: [
            for (final label in weekdayLabels)
              Expanded(
                child: Text(
                  label,
                  textAlign: TextAlign.center,
                  style: context.text.labelMedium
                      ?.copyWith(color: AppColors.onSurfaceVariant),
                ),
              ),
          ],
        ),
        const SizedBox(height: 8),
        Expanded(
          child: GridView.builder(
            padding: EdgeInsets.zero,
            physics: const NeverScrollableScrollPhysics(),
            gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
              crossAxisCount: _columnCount,
              childAspectRatio: _cellAspectRatio,
            ),
            itemCount: leadingBlanks + days.length,
            itemBuilder: (context, index) {
              if (index < leadingBlanks) return const SizedBox.shrink();

              final day = days[index - leadingBlanks];
              return _DayCell(day: day, onTap: () => onDayTap(day));
            },
          ),
        ),
      ],
    );
  }
}

class _DayCell extends StatelessWidget {
  const _DayCell({required this.day, required this.onTap});

  static const _circleSize = 32.0;

  final CalendarDay day;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              width: _circleSize,
              height: _circleSize,
              alignment: Alignment.center,
              decoration: BoxDecoration(
                color: day.isToday ? AppColors.primary : Colors.transparent,
                shape: BoxShape.circle,
              ),
              child: Text(
                '${day.date.day}',
                style: context.text.bodyMedium?.copyWith(
                  fontWeight: day.isToday ? FontWeight.w600 : null,
                  color:
                      day.isToday ? AppColors.onPrimary : AppColors.onSurface,
                ),
              ),
            ),
            const SizedBox(height: 4),
            CycleDayMarker(day: day),
          ],
        ),
      );
}

class _Legend extends StatelessWidget {
  const _Legend();

  @override
  Widget build(BuildContext context) => const SoftShadowCard(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            _LegendRow(
              icon: Icon(
                Icons.water_drop,
                size: 12,
                color: AppColors.bleeding,
              ),
              label: 'Kanama kaydı (damla, kan rengini alır)',
            ),
            SizedBox(height: 12),
            _LegendRow(
              icon: _LegendDot(color: AppColors.spotting, size: 6),
              label: 'Lekelenme',
            ),
            SizedBox(height: 12),
            _LegendRow(
              icon: _LegendDot(color: AppColors.primary, size: 6),
              label: 'Tahmini adet günü',
            ),
            SizedBox(height: 12),
            _LegendRow(
              icon: _LegendDot(color: AppColors.secondaryContainer, size: 6),
              label: 'Kayıt var',
            ),
          ],
        ),
      );
}

class _LegendRow extends StatelessWidget {
  const _LegendRow({required this.icon, required this.label});

  final Widget icon;
  final String label;

  @override
  Widget build(BuildContext context) => Row(
        children: [
          SizedBox(width: 16, child: Center(child: icon)),
          const SizedBox(width: 8),
          // Uzun etiketler dar ekranda satıra sığmayıp taşmasın.
          Expanded(
            child: Text(
              label,
              style: context.text.bodyMedium
                  ?.copyWith(color: AppColors.onSurfaceVariant),
            ),
          ),
        ],
      );
}

class _LegendDot extends StatelessWidget {
  const _LegendDot({required this.color, required this.size});

  final Color color;
  final double size;

  @override
  Widget build(BuildContext context) => Container(
        width: size,
        height: size,
        decoration: BoxDecoration(color: color, shape: BoxShape.circle),
      );
}
