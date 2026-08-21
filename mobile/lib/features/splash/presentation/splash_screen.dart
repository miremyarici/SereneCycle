import 'package:flutter/material.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/widgets/app_logo.dart';

/// Açılış ekranı. Kalıcı bir sayfa değil: güvenli depodaki refresh token'la
/// oturum geri kurulurken (`/me` denenirken) görünür, sonuç belli olunca
/// router'daki yönlendirme kullanıcıyı giriş ekranına, onboarding'e ya da
/// ana sayfaya alır.
class SplashScreen extends StatelessWidget {
  const SplashScreen({super.key});

  @override
  Widget build(BuildContext context) => const Scaffold(
        backgroundColor: AppColors.background,
        body: Center(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              AppLogo(),
              SizedBox(height: 40),
              SizedBox(
                width: 24,
                height: 24,
                child: CircularProgressIndicator(
                  strokeWidth: 2,
                  color: AppColors.primary,
                ),
              ),
            ],
          ),
        ),
      );
}
