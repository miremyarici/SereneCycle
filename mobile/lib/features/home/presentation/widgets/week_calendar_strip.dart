import 'package:flutter/material.dart';

import '../../../../core/api/models.dart';
import '../../../../core/theme/app_colors.dart';
import 'cycle_day_marker.dart';

/// Yatay haftalık takvim: bugün vurgulu, günlerin altında kanama/lekelenme
/// ve tahmin işaretleri ([CycleDayMarker]).
class WeekCalendarStrip extends StatelessWidget {
  const WeekCalendarStrip({required this.days, this.onDayTap, super.key});

  final List<CalendarDay> days;
  final void Function(CalendarDay day)? onDayTap;

  static const _weekdayLabels = [
    'Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt', 'Paz',
  ];

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: days.map((day) {
        // DateTime.weekday: 1 = Pazartesi.
        final label = _weekdayLabels[day.date.weekday - 1];

        return Expanded(
          child: InkWell(
            onTap: onDayTap == null ? null : () => onDayTap!(day),
            borderRadius: BorderRadius.circular(12),
            child: Padding(
              padding: const EdgeInsets.symmetric(vertical: 4),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(
                    label,
                    style: TextStyle(
                      fontSize: 12,
                      fontWeight:
                          day.isToday ? FontWeight.w700 : FontWeight.w500,
                      color: day.isToday
                          ? AppColors.primary
                          : AppColors.onSurfaceVariant,
                    ),
                  ),
                  const SizedBox(height: 8),
                  Container(
                    width: 36,
                    height: 36,
                    alignment: Alignment.center,
                    decoration: BoxDecoration(
                      color: day.isToday
                          ? AppColors.primary
                          : Colors.transparent,
                      shape: BoxShape.circle,
                    ),
                    child: Text(
                      '${day.date.day}',
                      style: TextStyle(
                        fontSize: 15,
                        fontWeight: day.isToday
                            ? FontWeight.w600
                            : FontWeight.w400,
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
          ),
        );
      }).toList(),
    );
  }
}
