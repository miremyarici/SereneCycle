import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/api/api_client.dart';
import '../../../core/providers/app_providers.dart';
import '../../../core/router/route_paths.dart';
import '../../../core/validation/password_validator.dart';
import '../../../core/widgets/app_snack_bar.dart';
import '../../../core/widgets/pill_button.dart';
import '../../../core/widgets/soft_shadow_card.dart';
import '../../../core/widgets/underlined_text_field.dart';
import 'widgets/auth_header.dart';
import 'widgets/auth_page.dart';
import 'widgets/password_field.dart';

class NewPasswordScreen extends ConsumerStatefulWidget {
  const NewPasswordScreen({required this.email, super.key});

  final String email;

  @override
  ConsumerState<NewPasswordScreen> createState() => _NewPasswordScreenState();
}

class _NewPasswordScreenState extends ConsumerState<NewPasswordScreen> {
  static const _codeLength = 6;

  final _formKey = GlobalKey<FormState>();
  final _codeController = TextEditingController();
  final _passwordController = TextEditingController();
  final _confirmController = TextEditingController();
  bool _isLoading = false;

  @override
  void dispose() {
    _codeController.dispose();
    _passwordController.dispose();
    _confirmController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;

    if (widget.email.isEmpty) {
      context.showError(
        'E-posta bulunamadı, lütfen "Şifremi unuttum" akışını baştan başlat.',
      );
      return;
    }

    setState(() => _isLoading = true);

    try {
      await ref.read(sereneApiProvider).resetPassword(
            email: widget.email,
            code: _codeController.text,
            newPassword: _passwordController.text,
          );

      if (mounted) {
        context.showMessage('Şifren güncellendi, giriş yapabilirsin.');
        context.go(RoutePaths.login);
      }
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
      child: SoftShadowCard(
        padding: const EdgeInsets.all(24),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const AuthHeader(
                title: 'Yeni şifre belirle',
                subtitle: 'Hesabın için güçlü, daha önce kullanmadığın bir '
                    'şifre seç.',
              ),
              const SizedBox(height: 32),
              UnderlinedTextField(
                label: 'Doğrulama Kodu',
                controller: _codeController,
                hintText: '$_codeLength haneli kod',
                keyboardType: TextInputType.number,
                textInputAction: TextInputAction.next,
                inputFormatters: [
                  FilteringTextInputFormatter.digitsOnly,
                  LengthLimitingTextInputFormatter(_codeLength),
                ],
                validator: (value) => (value == null ||
                        value.length != _codeLength)
                    ? '$_codeLength haneli kodu gir'
                    : null,
              ),
              const SizedBox(height: 20),
              PasswordField(
                label: 'Yeni Şifre',
                controller: _passwordController,
                textInputAction: TextInputAction.next,
                autofillHints: const [AutofillHints.newPassword],
                validator: validatePassword,
              ),
              const SizedBox(height: 20),
              PasswordField(
                label: 'Şifreyi Tekrar Gir',
                controller: _confirmController,
                textInputAction: TextInputAction.done,
                validator: (value) => value != _passwordController.text
                    ? 'Şifreler eşleşmiyor'
                    : null,
              ),
              const SizedBox(height: 32),
              PillButton(
                label: 'Şifreyi Sıfırla',
                filled: true,
                onPressed: _submit,
                isLoading: _isLoading,
              ),
            ],
          ),
        ),
      ),
    );
  }
}
