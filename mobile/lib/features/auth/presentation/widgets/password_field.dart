import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/widgets/underlined_text_field.dart';

/// Göz ikonuyla gizle/göster yapan parola alanı. Görünürlük durumu ekranın
/// değil alanın kendi meselesi olduğu için state burada duruyor; dört yerde
/// tekrarlanan `_obscurePassword` alanlarını kaldırdı.
class PasswordField extends StatefulWidget {
  const PasswordField({
    required this.label,
    required this.controller,
    required this.validator,
    this.hintText = '••••••••',
    this.textInputAction,
    this.autofillHints,
    super.key,
  });

  final String label;
  final TextEditingController controller;
  final String? Function(String?) validator;
  final String hintText;
  final TextInputAction? textInputAction;
  final Iterable<String>? autofillHints;

  @override
  State<PasswordField> createState() => _PasswordFieldState();
}

class _PasswordFieldState extends State<PasswordField> {
  bool _isObscured = true;

  @override
  Widget build(BuildContext context) => UnderlinedTextField(
        label: widget.label,
        controller: widget.controller,
        hintText: widget.hintText,
        obscureText: _isObscured,
        textInputAction: widget.textInputAction,
        autofillHints: widget.autofillHints,
        validator: widget.validator,
        suffixIcon: IconButton(
          tooltip: _isObscured ? 'Parolayı göster' : 'Parolayı gizle',
          icon: Icon(
            _isObscured
                ? Icons.visibility_off_outlined
                : Icons.visibility_outlined,
            size: 20,
            color: AppColors.outline,
          ),
          onPressed: () => setState(() => _isObscured = !_isObscured),
        ),
      );
}
