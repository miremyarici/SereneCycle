import 'dart:math' as math;

import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';

/// Tasarımdaki döngü ilerleme halkası: arkada soluk bir tam çember,
/// üstünde ilerlemeyi gösteren yay, ortada "7 / 28".
class CycleProgressRing extends StatelessWidget {
  const CycleProgressRing({
    required this.cycleDay,
    required this.cycleLength,
    required this.progress,
    super.key,
  });

  final int cycleDay;
  final int cycleLength;
  final double progress;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 192,
      height: 192,
      child: Stack(
        alignment: Alignment.center,
        children: [
          // Animasyon, açılışta halkanın dolmasını gösterir.
          TweenAnimationBuilder<double>(
            tween: Tween(begin: 0, end: progress),
            duration: const Duration(milliseconds: 900),
            curve: Curves.easeOutCubic,
            builder: (context, value, _) => CustomPaint(
              size: const Size.square(192),
              painter: _RingPainter(value),
            ),
          ),
          Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(
                '$cycleDay. gün',
                style: const TextStyle(
                  fontSize: 28,
                  height: 36 / 28,
                  fontWeight: FontWeight.w600,
                  color: AppColors.primary,
                ),
              ),
              Text(
                '$cycleLength günlük döngü',
                style: const TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.w600,
                  color: AppColors.onSurfaceVariant,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _RingPainter extends CustomPainter {
  const _RingPainter(this.progress);

  final double progress;

  static const _strokeWidth = 8.0;

  @override
  void paint(Canvas canvas, Size size) {
    final center = size.center(Offset.zero);
    final radius = (size.width - _strokeWidth) / 2;

    final track = Paint()
      ..style = PaintingStyle.stroke
      ..strokeWidth = _strokeWidth
      ..strokeCap = StrokeCap.round
      ..color = AppColors.outlineVariant;

    final fill = Paint()
      ..style = PaintingStyle.stroke
      ..strokeWidth = _strokeWidth
      ..strokeCap = StrokeCap.round
      ..color = AppColors.primaryContainer;

    canvas.drawCircle(center, radius, track);

    // 12 yönünden başla (-90°), saat yönünde ilerle.
    canvas.drawArc(
      Rect.fromCircle(center: center, radius: radius),
      -math.pi / 2,
      2 * math.pi * progress,
      false,
      fill,
    );
  }

  @override
  bool shouldRepaint(_RingPainter oldDelegate) =>
      oldDelegate.progress != progress;
}
