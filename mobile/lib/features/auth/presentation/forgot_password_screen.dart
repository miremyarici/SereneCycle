import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/api/api_client.dart';
import '../../../core/providers/app_providers.dart';
import '../../../core/router/route_paths.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/validation/email_validator.dart';
import '../../../core/widgets/app_snack_bar.dart';
import '../../../core/widgets/pill_button.dart';
import '../../../core/widgets/soft_shadow_card.dart';
import '../../../core/widgets/underlined_text_field.dart';
import 'widgets/auth_header.dart';
import 'widgets/auth_page.dart';
import 'widgets/auth_prompt.dart';

class ForgotPasswordScreen extends ConsumerStatefulWidget {
  const ForgotPasswordScreen({super.key});

  @override
  ConsumerState<ForgotPasswordScreen> createState() =>
      _ForgotPasswordScreenState();
}

class _ForgotPasswordScreenState extends ConsumerState<ForgotPasswordScreen> {
  final _formKey = GlobalKey<FormState>();
  final _emailController = TextEditingController();
  bool _isLoading = false;

  @override
  void dispose() {
    _emailController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;

    final email = _emailController.text.trim();
    setState(() => _isLoading = true);

    try {
      // Backend hangi adreslerin kayıtlı olduğunu sızdırmamak için bu uç
      // her zaman başarı döner; kod gerçekten gitmiş olabilir ya da
      // olmayabilir, kullanıcıya aynı şekilde devam ettiriyoruz.
      await ref.read(sereneApiProvider).forgotPassword(email);

      if (mounted) context.push(RoutePaths.newPassword, extra: email);
    } on ApiException catch (e) {
      if (mounted) context.showError(e.message);
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return AuthPage(
      hasBackButton: true,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          SoftShadowCard(
            padding: const EdgeInsets.all(24),
            child: Form(
              key: _formKey,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  const Center(child: _LockBadge()),
                  const SizedBox(height: 24),
                  const AuthHeader(
                    title: 'Şifreni mi unuttun?',
                    subtitle: 'Kayıtlı e-postanı gir, sana bir sıfırlama '
                        'kodu gönderelim.',
                  ),
                  const SizedBox(height: 32),
                  UnderlinedTextField(
                    label: 'E-posta',
                    controller: _emailController,
                    hintText: 'sen@ornek.com',
                    keyboardType: TextInputType.emailAddress,
                    textInputAction: TextInputAction.done,
                    autofillHints: const [AutofillHints.email],
                    validator: validateEmail,
                  ),
                  const SizedBox(height: 24),
                  PillButton(
                    label: 'Sıfırlama Kodu Gönder',
                    onPressed: _submit,
                    isLoading: _isLoading,
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 24),
          AuthPrompt(
            question: 'Şifreni hatırladın mı? ',
            actionLabel: 'Giriş Yap',
            onTap: () => context.go(RoutePaths.login),
          ),
        ],
      ),
    );
  }
}

class _LockBadge extends StatelessWidget {
  const _LockBadge();

  static const _size = 56.0;

  @override
  Widget build(BuildContext context) => Container(
        width: _size,
        height: _size,
        decoration: const BoxDecoration(
          color: AppColors.surfaceContainerLow,
          shape: BoxShape.circle,
        ),
        child: const Icon(Icons.lock_reset, size: 26, color: AppColors.primary),
      );
}
