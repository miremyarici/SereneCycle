import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/api/api_client.dart';
import '../../../core/providers/app_providers.dart';
import '../../../core/router/route_paths.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_typography.dart';
import '../../../core/widgets/app_snack_bar.dart';
import '../../../core/widgets/pill_button.dart';
import 'widgets/auth_header.dart';
import 'widgets/auth_page.dart';
import 'widgets/auth_prompt.dart';

class VerifyCodeScreen extends ConsumerStatefulWidget {
  const VerifyCodeScreen({required this.email, super.key});

  final String email;

  @override
  ConsumerState<VerifyCodeScreen> createState() => _VerifyCodeScreenState();
}

class _VerifyCodeScreenState extends ConsumerState<VerifyCodeScreen> {
  static const _digitCount = 6;
  static const _resendCooldown = 30;

  final _controllers =
      List.generate(_digitCount, (_) => TextEditingController());
  final _focusNodes = List.generate(_digitCount, (_) => FocusNode());

  Timer? _timer;
  int _secondsLeft = 0;
  bool _isSubmitting = false;

  /// Her build'de altı denetleyiciyi birleştirmemek için durum burada
  /// tutuluyor; yalnızca bir hane değiştiğinde güncelleniyor.
  bool _isCodeComplete = false;

  String get _code => _controllers.map((c) => c.text).join();

  @override
  void dispose() {
    _timer?.cancel();
    for (final controller in _controllers) {
      controller.dispose();
    }
    for (final node in _focusNodes) {
      node.dispose();
    }
    super.dispose();
  }

  void _onDigitChanged(int index, String value) {
    if (value.isNotEmpty && index < _digitCount - 1) {
      _focusNodes[index + 1].requestFocus();
    } else if (value.isEmpty && index > 0) {
      _focusNodes[index - 1].requestFocus();
    }

    setState(() => _isCodeComplete = _code.length == _digitCount);
  }

  Future<void> _startResendCooldown() async {
    setState(() => _secondsLeft = _resendCooldown);
    _timer?.cancel();
    _timer = Timer.periodic(const Duration(seconds: 1), (timer) {
      if (_secondsLeft <= 1) {
        timer.cancel();
        setState(() => _secondsLeft = 0);
      } else {
        setState(() => _secondsLeft--);
      }
    });

    try {
      await ref.read(sereneApiProvider).resendCode(widget.email);
    } on ApiException catch (e) {
      if (mounted) context.showError(e.message);
    }
  }

  Future<void> _submit() async {
    if (widget.email.isEmpty) {
      context.showError('E-posta bulunamadı, lütfen tekrar kayıt ol.');
      return;
    }

    setState(() => _isSubmitting = true);

    try {
      final response =
          await ref.read(sereneApiProvider).verifyCode(widget.email, _code);

      await ref
          .read(authControllerProvider.notifier)
          .completeVerification(response);

      if (mounted) {
        context.go(
          response.user.hasCompletedOnboarding
              ? RoutePaths.home
              : RoutePaths.onboarding,
        );
      }
    } on ApiException catch (e) {
      if (mounted) context.showError(e.message);
    } finally {
      if (mounted) setState(() => _isSubmitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return AuthPage(
      hasBackButton: true,
      isCentered: false,
      child: Column(
        children: [
          const _MailBadge(),
          const SizedBox(height: 24),
          const AuthHeader(
            title: 'E-postanı doğrula',
            subtitle: 'E-posta adresine 6 haneli bir doğrulama kodu '
                'gönderdik. Devam etmek için aşağıya gir.',
          ),
          const SizedBox(height: 40),
          Row(
            children: [
              for (var index = 0; index < _digitCount; index++)
                Expanded(
                  child: Padding(
                    padding: EdgeInsets.only(
                      left: index == 0 ? 0 : 4,
                      right: index == _digitCount - 1 ? 0 : 4,
                    ),
                    child: _CodeDigitField(
                      controller: _controllers[index],
                      focusNode: _focusNodes[index],
                      autofocus: index == 0,
                      onChanged: (value) => _onDigitChanged(index, value),
                    ),
                  ),
                ),
            ],
          ),
          const SizedBox(height: 24),
          if (_secondsLeft > 0)
            Text(
              '$_secondsLeft saniye sonra tekrar gönderebilirsin',
              style: context.text.bodySmall
                  ?.copyWith(color: AppColors.outline),
            )
          else
            AuthPrompt(
              question: 'Kod gelmedi mi? ',
              actionLabel: 'Tekrar gönder',
              isUnderlined: false,
              onTap: _startResendCooldown,
            ),
          const SizedBox(height: 40),
          PillButton(
            label: 'Doğrula',
            filled: true,
            isLoading: _isSubmitting,
            onPressed: _isCodeComplete ? _submit : null,
          ),
        ],
      ),
    );
  }
}

/// Tek haneli kod kutusu.
class _CodeDigitField extends StatelessWidget {
  const _CodeDigitField({
    required this.controller,
    required this.focusNode,
    required this.autofocus,
    required this.onChanged,
  });

  static const _height = 64.0;
  static const _radius = 16.0;
  static const _digitSize = 20.0;

  final TextEditingController controller;
  final FocusNode focusNode;
  final bool autofocus;
  final ValueChanged<String> onChanged;

  @override
  Widget build(BuildContext context) => SizedBox(
        height: _height,
        child: TextField(
          controller: controller,
          focusNode: focusNode,
          autofocus: autofocus,
          textAlign: TextAlign.center,
          keyboardType: TextInputType.number,
          maxLength: 1,
          inputFormatters: [FilteringTextInputFormatter.digitsOnly],
          onChanged: onChanged,
          style: context.text.bodyLarge?.copyWith(
            fontSize: _digitSize,
            fontWeight: FontWeight.w600,
            color: AppColors.onSurface,
          ),
          decoration: InputDecoration(
            counterText: '',
            filled: true,
            fillColor: AppColors.surfaceContainerLowest,
            contentPadding: EdgeInsets.zero,
            enabledBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(_radius),
              borderSide: const BorderSide(color: AppColors.outlineVariant),
            ),
            focusedBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(_radius),
              borderSide: const BorderSide(color: AppColors.primary, width: 2),
            ),
          ),
        ),
      );
}

class _MailBadge extends StatelessWidget {
  const _MailBadge();

  static const _size = 64.0;

  @override
  Widget build(BuildContext context) => Container(
        width: _size,
        height: _size,
        decoration: const BoxDecoration(
          color: AppColors.surfaceContainerLow,
          shape: BoxShape.circle,
        ),
        child: const Icon(
          Icons.mark_email_read_outlined,
          size: 30,
          color: AppColors.primary,
        ),
      );
}
