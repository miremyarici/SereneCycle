import 'package:flutter/material.dart';

import '../api/api_client.dart';
import '../theme/app_colors.dart';
import '../theme/app_typography.dart';

/// Doğrulama, yükleme durumu ve hata gösterimini tek yerde toplayan pop-up.
/// İsim/e-posta/parola değişikliği ve hesap silme aynı iskeleti kullanıyor:
/// üçü de "bir form doldur, sunucuya gönder, sonucu göster" akışı.
///
/// [onSubmit] başarılıysa döndürdüğü metin snackbar'da gösterilir; hata
/// mesajı pop-up'ın içinde kalır ki kullanıcı formu kaybetmesin.
class FormDialog<T> extends StatefulWidget {
  const FormDialog({
    required this.title,
    required this.submitLabel,
    required this.fields,
    required this.onSubmit,
    this.description,
    this.resultOnSuccess,
    this.isDestructive = false,
    super.key,
  });

  final String title;
  final String? description;
  final String submitLabel;
  final List<Widget> Function() fields;
  final Future<String> Function() onSubmit;
  final T? resultOnSuccess;

  /// Geri alınamaz işlemler için: onay düğmesi hata rengine döner.
  final bool isDestructive;

  @override
  State<FormDialog<T>> createState() => _FormDialogState<T>();
}

class _FormDialogState<T> extends State<FormDialog<T>> {
  final _formKey = GlobalKey<FormState>();
  bool _isSubmitting = false;
  String? _error;

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;

    setState(() {
      _isSubmitting = true;
      _error = null;
    });

    // Pop'tan sonra bu context ölüyor; messenger'ı önceden alıyoruz.
    final messenger = ScaffoldMessenger.of(context);
    final navigator = Navigator.of(context);

    try {
      final message = await widget.onSubmit();

      // Hesap silme gibi durumlarda yönlendirme pop-up'ı zaten kaldırmış
      // olabilir; o hâlde poplamaya çalışmıyoruz.
      if (!mounted) return;

      navigator.pop(widget.resultOnSuccess);
      messenger.showSnackBar(SnackBar(content: Text(message)));
    } on ApiException catch (e) {
      if (mounted) {
        setState(() {
          _error = e.message;
          _isSubmitting = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final accent = widget.isDestructive ? AppColors.error : AppColors.primary;

    return AlertDialog(
      backgroundColor: AppColors.surfaceContainerLowest,
      title: Text(
        widget.title,
        style: context.text.headlineSmall?.copyWith(color: accent),
      ),
      content: Form(
        key: _formKey,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            if (widget.description != null) ...[
              Text(
                widget.description!,
                style: context.text.bodyMedium
                    ?.copyWith(color: AppColors.onSurfaceVariant),
              ),
              const SizedBox(height: 16),
            ],
            ...widget.fields(),
            if (_error != null) ...[
              const SizedBox(height: 16),
              Text(
                _error!,
                style:
                    context.text.bodyMedium?.copyWith(color: AppColors.error),
              ),
            ],
          ],
        ),
      ),
      actions: [
        TextButton(
          onPressed: _isSubmitting ? null : () => Navigator.of(context).pop(),
          style: TextButton.styleFrom(
            foregroundColor: AppColors.onSurfaceVariant,
          ),
          child: const Text('Vazgeç'),
        ),
        FilledButton(
          onPressed: _isSubmitting ? null : _submit,
          style: FilledButton.styleFrom(
            backgroundColor: accent,
            foregroundColor: AppColors.onPrimary,
            shape: const StadiumBorder(),
          ),
          child: _isSubmitting
              ? const SizedBox(
                  width: 18,
                  height: 18,
                  child: CircularProgressIndicator(
                    strokeWidth: 2,
                    color: AppColors.onPrimary,
                  ),
                )
              : Text(widget.submitLabel),
        ),
      ],
    );
  }
}
