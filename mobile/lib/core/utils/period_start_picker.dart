import 'package:flutter/material.dart';

/// "Son adet başlangıcı" tarih seçicisi. Onboarding ve döngü ayarları aynı
/// sınırlarla açmak zorunda: gelecek tarihi backend reddediyor, bir yıldan
/// eskisi de tahmin için anlamlı değil.
Future<DateTime?> pickPeriodStartDate(
  BuildContext context, {
  required DateTime? initialDate,
}) {
  const yearSpan = 1;

  final now = DateTime.now();
  final today = DateTime(now.year, now.month, now.day);

  return showDatePicker(
    context: context,
    initialDate: initialDate ?? today,
    firstDate: DateTime(today.year - yearSpan, today.month, today.day),
    lastDate: today,
    helpText: 'Son adet başlangıcı',
    confirmText: 'Seç',
    cancelText: 'Vazgeç',
  );
}
