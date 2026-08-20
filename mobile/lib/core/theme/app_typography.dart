import 'package:flutter/material.dart';

/// Uygulamanın iki yazı ailesi ve tam tipografi ölçeği tek yerde.
///
/// Kural basit: **başlık** niteliğindeki her şey (display / headline / title
/// yuvaları, ekran başlıkları, kart başlıkları, bölüm başlıkları) Merriweather
/// Bold; **gövde ve etiket** niteliğindeki her şey (body / label yuvaları,
/// açıklama paragrafları, form yardım metinleri, buton metinleri) Helvetica.
///
/// Merriweather `assets/fonts/` altında paketlenir (SIL OFL). Çalışma
/// zamanında indirme yapılmaz; böylece çevrimdışı cihazda ve widget
/// testlerinde de aynı şekilde çözülür.
///
/// Helvetica'nın serbestçe dağıtılabilen bir dosyası yok ve Google Fonts'ta da
/// bulunmuyor. Aile adı bilerek `Helvetica` olarak korunuyor: cihazda kuruluysa
/// gerçek Helvetica kullanılır, değilse [bodyFallback] zinciri devreye girer.
/// Sessizce başka bir aileyle değiştirmiyoruz.
abstract class AppTypography {
  static const headingFamily = 'Merriweather';
  static const bodyFamily = 'Helvetica';

  /// Helvetica bulunamazsa sırasıyla denenecek aileler.
  static const bodyFallback = <String>['Helvetica Neue', 'Arial', 'Roboto'];

  /// Tasarımdaki satır yükseklikleri "px / px" olarak veriliyor; [TextStyle]
  /// oransal beklediği için bölme burada bir kez yapılıyor.
  static TextStyle _heading({
    required double size,
    required double lineHeight,
    double letterSpacing = 0,
  }) =>
      TextStyle(
        fontFamily: headingFamily,
        fontWeight: FontWeight.w700,
        fontSize: size,
        height: lineHeight / size,
        letterSpacing: letterSpacing,
      );

  static TextStyle _body({
    required double size,
    required double lineHeight,
    FontWeight weight = FontWeight.w400,
    double letterSpacing = 0,
  }) =>
      TextStyle(
        fontFamily: bodyFamily,
        fontFamilyFallback: bodyFallback,
        fontWeight: weight,
        fontSize: size,
        height: lineHeight / size,
        letterSpacing: letterSpacing,
      );

  /// Renk bilerek boş bırakılıyor: [ThemeData] kendi varsayılan renklerini
  /// (colorScheme.onSurface) bunun üstüne birleştiriyor, çağıran yerler de
  /// gerektiğinde `copyWith(color: ...)` ile kendi tonunu veriyor.
  static final TextTheme textTheme = TextTheme(
    displayLarge: _heading(size: 36, lineHeight: 44),
    displayMedium: _heading(size: 32, lineHeight: 40),
    displaySmall: _heading(size: 28, lineHeight: 36),
    headlineLarge: _heading(size: 28, lineHeight: 34),
    headlineMedium: _heading(size: 24, lineHeight: 32),
    headlineSmall: _heading(size: 20, lineHeight: 28),
    titleLarge: _heading(size: 18, lineHeight: 24),
    titleMedium: _heading(size: 16, lineHeight: 22),
    titleSmall: _heading(size: 14, lineHeight: 20),
    bodyLarge: _body(size: 16, lineHeight: 24),
    bodyMedium: _body(size: 14, lineHeight: 20),
    bodySmall: _body(size: 12, lineHeight: 16),
    labelLarge: _body(
      size: 14,
      lineHeight: 20,
      weight: FontWeight.w600,
      letterSpacing: 0.14,
    ),
    labelMedium: _body(size: 12, lineHeight: 16, weight: FontWeight.w500),
    labelSmall: _body(size: 11, lineHeight: 16, weight: FontWeight.w600),
  );

  /// Büyük harfli bölüm başlıkları ("DÖNGÜ AYARLARI") tek bir yerden gelsin.
  static final TextStyle sectionHeader =
      textTheme.titleSmall!.copyWith(letterSpacing: 1.2);
}

/// `Theme.of(context).textTheme` zincirini her çağrı yerinde tekrarlamamak
/// için kısa yol.
extension AppTextTheme on BuildContext {
  TextTheme get text => Theme.of(this).textTheme;
}
