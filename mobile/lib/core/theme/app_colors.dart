import 'package:flutter/material.dart';

/// Google Stitch tasarım referansından çıkarılan Material Design 3
/// renk tokenları (sıcak kahverengi-şeftali tema). Kaynak:
/// C:\Users\HP\.claude\plans\sana-imdi-bu-projenin-greedy-river.md
abstract class AppColors {
  static const primary = Color(0xFF63432E);
  static const onPrimary = Color(0xFFFFFFFF);
  static const primaryContainer = Color(0xFF7D5A44);
  static const onPrimaryContainer = Color(0xFFFFD5BD);

  static const secondary = Color(0xFF715A45);
  static const secondaryContainer = Color(0xFFFADABF);

  static const tertiary = Color(0xFF52483F);
  static const tertiaryContainer = Color(0xFF6A6056);

  static const background = Color(0xFFFFF8F6);
  static const surface = Color(0xFFFFF8F6);
  static const surfaceContainerLowest = Color(0xFFFFFFFF);
  static const surfaceContainerLow = Color(0xFFFFF1EB);
  static const surfaceContainer = Color(0xFFFFEAE0);
  static const surfaceContainerHigh = Color(0xFFFFE2D6);
  static const surfaceContainerHighest = Color(0xFFFFDBCB);

  static const onSurface = Color(0xFF29170E);
  static const onSurfaceVariant = Color(0xFF50443E);
  static const outline = Color(0xFF82746D);
  static const outlineVariant = Color(0xFFD4C3BB);

  static const error = Color(0xFFBA1A1A);
  static const errorContainer = Color(0xFFFFDAD6);

  /// Takvimdeki kanama damlası ve lekelenme noktası. Paletteki kahverengi
  /// tonlarından ayrışmaları gerektiği için kendi tokenları var.
  static const bleeding = Color(0xFFC2405A);
  static const spotting = Color(0xFFE0913B);
}
