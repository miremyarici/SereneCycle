import 'package:flutter/material.dart';

import '../theme/app_colors.dart';

/// Ekranların çoğu aynı iki satırı tekrarlıyordu: bilgi mesajı için düz
/// snackbar, hata için kırmızı zeminli snackbar. İkisi de buradan geçiyor ki
/// hata rengi tek yerde dursun.
extension AppSnackBar on BuildContext {
  void showMessage(String message) => _show(message);

  void showError(String message) => _show(message, isError: true);

  void _show(String message, {bool isError = false}) {
    ScaffoldMessenger.of(this).showSnackBar(
      SnackBar(
        content: Text(message),
        backgroundColor: isError ? AppColors.error : null,
      ),
    );
  }
}
