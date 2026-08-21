import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/api/api_client.dart';
import '../../../core/providers/app_providers.dart';
import '../../../core/router/route_paths.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_typography.dart';
import '../../../core/validation/email_validator.dart';
import '../../../core/widgets/app_logo.dart';
import '../../../core/widgets/app_snack_bar.dart';
import '../../../core/widgets/pill_button.dart';
import '../../../core/widgets/soft_shadow_card.dart';
import '../../../core/widgets/underlined_text_field.dart';
import 'widgets/auth_header.dart';
import 'widgets/auth_page.dart';
import 'widgets/auth_prompt.dart';
import 'widgets/password_field.dart';

class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key});

  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  bool _isLoading = false;

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;

    setState(() => _isLoading = true);

    try {
      // Giriş sonrası yönlendirmeyi router üstleniyor: "onboarding bitti mi"
      // sorusunun tek cevaplandığı yer orası.
      await ref.read(authControllerProvider.notifier).signIn(
            _emailController.text.trim(),
            _passwordController.text,
          );
    } on ApiException catch (e) {
      if (mounted) context.showError(e.message);
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return AuthPage(
      padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 32),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const _Wordmark(),
          SoftShadowCard(
            child: _LoginForm(
              formKey: _formKey,
              emailController: _emailController,
              passwordController: _passwordController,
              isLoading: _isLoading,
              onSubmit: _submit,
            ),
          ),
          const SizedBox(height: 24),
          TextButton(
            onPressed: () => context.push(RoutePaths.forgotPassword),
            child: Text(
              'Şifremi unuttum',
              style: context.text.bodyLarge?.copyWith(
                color: AppColors.primary,
                decoration: TextDecoration.underline,
                decorationColor: AppColors.primary,
              ),
            ),
          ),
          AuthPrompt(
            question: 'Yeni misin? ',
            actionLabel: 'Kayıt Ol',
            onTap: () => context.push(RoutePaths.signUp),
          ),
        ],
      ),
    );
  }
}

/// Giriş formu: alanlar, doğrulama ve gönder düğmesi.
class _LoginForm extends StatelessWidget {
  const _LoginForm({
    required this.formKey,
    required this.emailController,
    required this.passwordController,
    required this.isLoading,
    required this.onSubmit,
  });

  final GlobalKey<FormState> formKey;
  final TextEditingController emailController;
  final TextEditingController passwordController;
  final bool isLoading;
  final VoidCallback onSubmit;

  @override
  Widget build(BuildContext context) => Form(
        key: formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const AuthHeader(
              title: 'Tekrar hoş geldin',
              subtitle: 'Yolculuğuna devam etmek için giriş yap.',
            ),
            const SizedBox(height: 24),
            UnderlinedTextField(
              label: 'E-posta',
              controller: emailController,
              hintText: 'sen@ornek.com',
              keyboardType: TextInputType.emailAddress,
              textInputAction: TextInputAction.next,
              autofillHints: const [AutofillHints.email],
              validator: validateEmail,
            ),
            const SizedBox(height: 20),
            PasswordField(
              label: 'Şifre',
              controller: passwordController,
              textInputAction: TextInputAction.done,
              autofillHints: const [AutofillHints.password],
              validator: (value) =>
                  (value == null || value.isEmpty) ? 'Şifre gerekli' : null,
            ),
            const SizedBox(height: 32),
            PillButton(
              label: 'Giriş Yap',
              onPressed: onSubmit,
              isLoading: isLoading,
            ),
          ],
        ),
      );
}

/// Giriş ekranının tepesindeki uygulama logosu.
class _Wordmark extends StatelessWidget {
  const _Wordmark();

  @override
  Widget build(BuildContext context) => const Padding(
        padding: EdgeInsets.only(bottom: 32),
        child: AppLogo(),
      );
}
