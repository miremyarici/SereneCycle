import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../theme/app_colors.dart';
import '../theme/app_typography.dart';

/// Auth ekranlarındaki minimal input: üstte küçük etiket,
/// altta tek çizgi (tasarımdaki `border-b` deseni).
class UnderlinedTextField extends StatelessWidget {
  const UnderlinedTextField({
    required this.label,
    this.controller,
    this.hintText,
    this.keyboardType,
    this.obscureText = false,
    this.suffixIcon,
    this.validator,
    this.textInputAction,
    this.autofillHints,
    this.inputFormatters,
    super.key,
  });

  final String label;
  final TextEditingController? controller;
  final String? hintText;
  final TextInputType? keyboardType;
  final bool obscureText;
  final Widget? suffixIcon;
  final String? Function(String?)? validator;
  final TextInputAction? textInputAction;
  final Iterable<String>? autofillHints;
  final List<TextInputFormatter>? inputFormatters;

  static const _hintOpacity = 0.5;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.only(left: 4, bottom: 4),
          child: Text(
            label,
            style: context.text.labelMedium
                ?.copyWith(color: AppColors.onSurfaceVariant),
          ),
        ),
        TextFormField(
          controller: controller,
          keyboardType: keyboardType,
          obscureText: obscureText,
          validator: validator,
          textInputAction: textInputAction,
          autofillHints: autofillHints,
          inputFormatters: inputFormatters,
          style: context.text.bodyLarge?.copyWith(
            color: AppColors.onSurface,
          ),
          decoration: InputDecoration(
            hintText: hintText,
            hintStyle: context.text.bodyLarge?.copyWith(
              color: AppColors.onSurfaceVariant
                  .withValues(alpha: _hintOpacity),
            ),
            suffixIcon: suffixIcon,
            isDense: true,
            contentPadding: const EdgeInsets.symmetric(
              horizontal: 4,
              vertical: 8,
            ),
            enabledBorder: const UnderlineInputBorder(
              borderSide: BorderSide(color: AppColors.outlineVariant),
            ),
            focusedBorder: const UnderlineInputBorder(
              borderSide: BorderSide(color: AppColors.primary),
            ),
          ),
        ),
      ],
    );
  }
}
