import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/api/api_client.dart';
import '../../../core/providers/app_providers.dart';
import '../../../core/router/route_paths.dart';
import '../../../core/validation/email_validator.dart';
import '../../../core/validation/password_validator.dart';
import '../../../core/widgets/app_snack_bar.dart';
import '../../../core/widgets/pill_button.dart';
import '../../../core/widgets/soft_shadow_card.dart';
import '../../../core/widgets/underlined_text_field.dart';
import 'widgets/auth_header.dart';
import 'widgets/auth_page.dart';
import 'widgets/auth_prompt.dart';
import 'widgets/password_field.dart';

class SignUpScreen extends ConsumerStatefulWidget {
  const SignUpScreen({super.key});

  @override
  ConsumerState<SignUpScreen> createState() => _SignUpScreenState();
}

class _SignUpScreenState extends ConsumerState<SignUpScreen> {
  final _formKey = GlobalKey<FormState>();
  final _nameController = TextEditingController();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  bool _isLoading = false;

  @override
  void dispose() {
    _nameController.dispose();
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;

    final email = _emailController.text.trim();
    setState(() => _isLoading = true);

    try {
      await ref.read(sereneApiProvider).register(
            name: _nameController.text.trim(),
            email: email,
            password: _passwordController.text,
          );

      if (mounted) context.push(RoutePaths.verifyCode, extra: email);
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
      title: 'Serene Cycle',
      padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 20),
      child: SoftShadowCard(
        padding: const EdgeInsets.all(24),
        child: _SignUpForm(
          formKey: _formKey,
          nameController: _nameController,
          emailController: _emailController,
          passwordController: _passwordController,
          isLoading: _isLoading,
          onSubmit: _submit,
        ),
      ),
    );
  }
}

/// Kayıt formu: isim, e-posta, parola ve giriş bağlantısı.
class _SignUpForm extends StatelessWidget {
  const _SignUpForm({
    required this.formKey,
    required this.nameController,
    required this.emailController,
    required this.passwordController,
    required this.isLoading,
    required this.onSubmit,
  });

  final GlobalKey<FormState> formKey;
  final TextEditingController nameController;
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
              title: 'Yolculuğuna başla',
              subtitle: 'Kendini tanımak için sakin bir alan oluştur.',
            ),
            const SizedBox(height: 32),
            UnderlinedTextField(
              label: 'Tercih ettiğin isim',
              controller: nameController,
              hintText: 'Örn. Elif',
              textInputAction: TextInputAction.next,
              autofillHints: const [AutofillHints.name],
              validator: (value) => (value == null || value.trim().isEmpty)
                  ? 'İsim gerekli'
                  : null,
            ),
            const SizedBox(height: 20),
            UnderlinedTextField(
              label: 'E-posta adresi',
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
              autofillHints: const [AutofillHints.newPassword],
              validator: validatePassword,
            ),
            const SizedBox(height: 32),
            PillButton(
              label: 'Hesap Oluştur',
              onPressed: onSubmit,
              isLoading: isLoading,
              trailingIcon: Icons.arrow_forward,
            ),
            const SizedBox(height: 24),
            AuthPrompt(
              question: 'Zaten hesabın var mı? ',
              actionLabel: 'Giriş yap',
              isUnderlined: false,
              onTap: () => context.go(RoutePaths.login),
            ),
          ],
        ),
      );
}
