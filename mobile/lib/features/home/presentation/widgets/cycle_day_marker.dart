import 'package:flutter/material.dart';

import '../../../../core/api/models.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/blood_colors.dart';

/// Takvimde bir günün altındaki işaretler. Haftalık şerit ve aylık takvim
/// aynı görsel dili kullansın diye tek yerde tanımlı.
///
/// Kanama kaydı olan gün nokta yerine içi dolu damla ile gösterilir ve damla
/// o güne girilen kan rengine göre boyanır (renk seçilmemişse varsayılan
/// ton). Lekelenme varsa damlanın hemen altında farklı renkte küçük bir
/// nokta çıkar. Kanama kaydı yoksa tahmini adet günü ve "kayıt var"
/// işaretleri eski hâliyle nokta olarak kalır.
class CycleDayMarker extends StatelessWidget {
  const CycleDayMarker({required this.day, super.key});

  final CalendarDay day;

  /// İşaretler değişse de gün numaraları hizada kalsın diye sabit yükseklik.
  static const height = 18.0;

  @override
  Widget build(BuildContext context) => SizedBox(
        height: height,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            SizedBox(
              height: 12,
              child: day.hasBleeding
                  ? Icon(
                      Icons.water_drop,
                      size: 12,
                      color: bloodDropColor(day.bloodColor),
                    )
                  : Center(child: _predictionDots(day)),
            ),
            const SizedBox(height: 2),
            SizedBox(
              height: 4,
              child: day.hasSpotting
                  ? const _Dot(color: AppColors.spotting, size: 4)
                  : null,
            ),
          ],
        ),
      );

  static Widget _predictionDots(CalendarDay day) => Row(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          if (day.isPeriodDay) const _Dot(color: AppColors.primary),
          if (day.isPeriodDay && day.hasLog) const SizedBox(width: 3),
          if (day.hasLog) const _Dot(color: AppColors.secondaryContainer),
        ],
      );
}

class _Dot extends StatelessWidget {
  const _Dot({required this.color, this.size = 6});

  final Color color;
  final double size;

  @override
  Widget build(BuildContext context) => Container(
        width: size,
        height: size,
        decoration: BoxDecoration(color: color, shape: BoxShape.circle),
      );
}
