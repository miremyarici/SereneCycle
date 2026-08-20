import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_typography.dart';

/// "Yeni misin? **Kayıt Ol**" biçimindeki alt bağlantılar. Dar ekranda
/// alt satıra kaysın diye [Wrap]; dört auth ekranında aynı desendi.
class AuthPrompt extends StatelessWidget {
  const AuthPrompt({
    required this.question,
    required this.actionLabel,
    required this.onTap,
    this.isUnderlined = true,
    super.key,
  });

  final String question;
  final String actionLabel;
  final VoidCallback onTap;
  final bool isUnderlined;

  @override
  Widget build(BuildContext context) => Wrap(
        alignment: WrapAlignment.center,
        crossAxisAlignment: WrapCrossAlignment.center,
        children: [
          Text(
            question,
            style: context.text.bodyMedium
                ?.copyWith(color: AppColors.onSurfaceVariant),
          ),
          GestureDetector(
            onTap: onTap,
            child: Text(
              actionLabel,
              style: context.text.labelLarge?.copyWith(
                color: AppColors.primary,
                decoration: isUnderlined ? TextDecoration.underline : null,
                decorationColor: AppColors.primary,
              ),
            ),
          ),
        ],
      );
}
