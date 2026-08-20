import 'dart:typed_data';

import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_typography.dart';

/// Profil fotoğrafı; henüz inmediyse ya da hiç yoksa baş harfler.
/// Profil ve hesap ayarları ekranları yalnızca çapta ayrışıyordu.
class AvatarCircle extends StatelessWidget {
  const AvatarCircle({
    required this.initials,
    required this.bytes,
    required this.diameter,
    required this.initialsSize,
    super.key,
  });

  final String initials;
  final Uint8List? bytes;
  final double diameter;
  final double initialsSize;

  @override
  Widget build(BuildContext context) {
    final image = bytes;

    return Container(
      width: diameter,
      height: diameter,
      alignment: Alignment.center,
      clipBehavior: Clip.antiAlias,
      decoration: const BoxDecoration(
        color: AppColors.primaryContainer,
        shape: BoxShape.circle,
      ),
      child: image == null
          ? Text(
              initials,
              style: context.text.displayLarge?.copyWith(
                fontSize: initialsSize,
                color: AppColors.onPrimaryContainer,
              ),
            )
          : Image.memory(
              image,
              width: diameter,
              height: diameter,
              fit: BoxFit.cover,
            ),
    );
  }
}
