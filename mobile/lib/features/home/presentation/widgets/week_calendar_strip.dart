import 'package:flutter/material.dart';

import '../../../../core/api/models.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_typography.dart';
import 'cycle_day_marker.dart';
import 'weekday_labels.dart';

/// Yatay haftalık takvim: bugün vurgulu, günlerin altında kanama/lekelenme
/// ve tahmin işaretleri ([CycleDayMarker]).
class WeekCalendarStrip extends StatelessWidget {
  const WeekCalendarStrip({required this.days, this.onDayTap, super.key});

  final List<CalendarDay> days;
  final void Function(CalendarDay day)? onDayTap;

  @override
  Widget build(BuildContext context) => Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          for (final day in days)
            Expanded(
              child: _DayColumn(
                day: day,
                onTap: onDayTap == null ? null : () => onDayTap!(day),
              ),
            ),
        ],
      );
}

/// Şeritteki tek gün: kısaltma, gün numarası ve işaretler.
class _DayColumn extends StatelessWidget {
  const _DayColumn({required this.day, required this.onTap});

  static const _circleSize = 36.0;
  static const _dayNumberSize = 15.0;

  final CalendarDay day;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) => InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.symmetric(vertical: 4),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(
                // DateTime.weekday: 1 = Pazartesi.
                weekdayLabels[day.date.weekday - 1],
                style: context.text.labelMedium?.copyWith(
                  fontWeight: day.isToday ? FontWeight.w700 : null,
                  color: day.isToday
                      ? AppColors.primary
                      : AppColors.onSurfaceVariant,
                ),
              ),
              const SizedBox(height: 8),
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
                    fontSize: _dayNumberSize,
                    fontWeight: day.isToday ? FontWeight.w600 : null,
                    color: day.isToday
                        ? AppColors.onPrimary
                        : AppColors.onSurface,
                  ),
                ),
              ),
              const SizedBox(height: 6),
              CycleDayMarker(day: day),
            ],
          ),
        ),
      );
}
