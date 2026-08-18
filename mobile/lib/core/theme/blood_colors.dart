import 'package:flutter/material.dart';

import '../api/models.dart';
import 'app_colors.dart';

/// Kan rengi seçeneklerinin ekrandaki görsel karşılığı. Hem adet kaydı
/// ekranındaki seçim çipleri hem takvimdeki damla ikonu buradan besleniyor
/// ki iki yerde farklı tonlar oluşmasın.
extension BloodColorSwatch on BloodColorOption {
  Color get swatch => switch (this) {
        BloodColorOption.red => const Color(0xFFC2263A),
        BloodColorOption.brown => const Color(0xFF7B4A2D),
        BloodColorOption.pink => const Color(0xFFE98BA6),
        BloodColorOption.black => const Color(0xFF2B2320),
        BloodColorOption.orange => const Color(0xFFE07A29),
        BloodColorOption.gray => const Color(0xFF9A918C),
      };
}

/// Kanama işaretli ama renk seçilmemiş günler için varsayılan damla rengi.
Color bloodDropColor(BloodColorOption? option) =>
    option?.swatch ?? AppColors.bleeding;
