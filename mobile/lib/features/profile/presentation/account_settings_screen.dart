import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:image_picker/image_picker.dart';

import '../../../core/api/api_client.dart';
import '../../../core/api/models.dart';
import '../../../core/providers/app_providers.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_typography.dart';
import '../../../core/validation/email_validator.dart';
import '../../../core/validation/password_validator.dart';
import '../../../core/widgets/app_snack_bar.dart';
import '../../../core/widgets/async_view.dart';
import '../../../core/widgets/avatar_circle.dart';
import '../../../core/widgets/circle_icon.dart';
import '../../../core/widgets/section_title.dart';
import '../../../core/widgets/soft_shadow_card.dart';
import '../../../core/widgets/underlined_text_field.dart';

/// Profil → "Profil bilgilerini düzenle". Fotoğraf burada, isim/e-posta/şifre
/// birer pop-up'ta değiştirilir.
class AccountSettingsScreen extends ConsumerWidget {
  const AccountSettingsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final profile = ref.watch(profileProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Profil Bilgileri'), centerTitle: true),
      body: AsyncView(
        value: profile,
        onRetry: () => ref.invalidate(profileProvider),
        builder: (user) => ListView(
          padding: const EdgeInsets.fromLTRB(24, 16, 24, 32),
          children: [
            _AvatarSection(user: user),
            const SizedBox(height: 24),
            _AccountCard(user: user),
            const SizedBox(height: 16),
            Text(
              'E-posta adresini değiştirdiğinde yeni adresine bir doğrulama '
              'kodu gönderilir; kod girilene kadar adresin değişmez.',
              style: context.text.bodyMedium
                  ?.copyWith(color: AppColors.onSurfaceVariant),
            ),
          ],
        ),
      ),
    );
  }
}

class _AccountCard extends ConsumerWidget {
  const _AccountCard({required this.user});

  final UserSummary user;

  @override
  Widget build(BuildContext context, WidgetRef ref) => SoftShadowCard(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const SectionTitle('HESAP'),
            const SizedBox(height: 8),
            _EditRow(
              icon: Icons.person_outline,
              label: 'İsim',
              value: user.name,
              onTap: () => _showNameDialog(context, ref, user),
            ),
            const Divider(height: 24, color: AppColors.outlineVariant),
            _EditRow(
              icon: Icons.mail_outline,
              label: 'E-posta',
              value: user.email,
              onTap: () => _showEmailDialog(context, ref),
            ),
            const Divider(height: 24, color: AppColors.outlineVariant),
            _EditRow(
              icon: Icons.lock_outline,
              label: 'Parola',
              value: '••••••••',
              onTap: () => _showPasswordDialog(context, ref),
            ),
          ],
        ),
      );
}

// --- Profil fotoğrafı ------------------------------------------------------

class _AvatarSection extends ConsumerStatefulWidget {
  const _AvatarSection({required this.user});

  final UserSummary user;

  @override
  ConsumerState<_AvatarSection> createState() => _AvatarSectionState();
}

class _AvatarSectionState extends ConsumerState<_AvatarSection> {
  bool _isBusy = false;

  /// Sunucu fotoğrafı satır içinde sakladığı için seçimde küçültüyoruz.
  static const _maxImageEdge = 512.0;

  static const _avatarSize = 112.0;
  static const _initialsSize = 40.0;

  static const _contentTypesByExtension = {
    'png': 'image/png',
    'webp': 'image/webp',
    'jpg': 'image/jpeg',
    'jpeg': 'image/jpeg',
  };

  Future<void> _pickAndUpload() async {
    final picked = await ImagePicker().pickImage(
      source: ImageSource.gallery,
      maxWidth: _maxImageEdge,
      maxHeight: _maxImageEdge,
    );

    if (picked == null) return;

    final extension = picked.name.split('.').last.toLowerCase();
    final contentType = picked.mimeType ?? _contentTypesByExtension[extension];

    if (contentType == null ||
        !_contentTypesByExtension.containsValue(contentType)) {
      if (mounted) {
        context.showError(
          'Yalnızca JPEG, PNG ve WebP fotoğraflar yüklenebilir.',
        );
      }
      return;
    }

    await _run(() async {
      final bytes = await picked.readAsBytes();
      await ref.read(sereneApiProvider).updateAvatar(
            contentType: contentType,
            bytes: bytes,
          );
    }, successMessage: 'Profil fotoğrafın güncellendi.');
  }

  Future<void> _remove() => _run(
        () => ref.read(sereneApiProvider).removeAvatar(),
        successMessage: 'Profil fotoğrafın kaldırıldı.',
      );

  Future<void> _run(
    Future<void> Function() action, {
    required String successMessage,
  }) async {
    setState(() => _isBusy = true);

    try {
      await action();
      // Fotoğraf `profileProvider`'a bağlı olduğu için tek invalidate yeter.
      ref.invalidate(profileProvider);
      if (mounted) context.showMessage(successMessage);
    } on ApiException catch (e) {
      if (mounted) context.showError(e.message);
    } finally {
      if (mounted) setState(() => _isBusy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final avatar = ref.watch(avatarProvider);

    return Column(
      children: [
        Stack(
          alignment: Alignment.bottomRight,
          children: [
            AvatarCircle(
              initials: widget.user.initials,
              bytes: avatar.value,
              diameter: _avatarSize,
              initialsSize: _initialsSize,
            ),
            IconButton.filled(
              onPressed: _isBusy ? null : _pickAndUpload,
              tooltip: 'Fotoğrafı değiştir',
              icon: const Icon(Icons.photo_camera_outlined, size: 18),
              style: IconButton.styleFrom(
                backgroundColor: AppColors.primary,
                foregroundColor: AppColors.onPrimary,
              ),
            ),
          ],
        ),
        const SizedBox(height: 12),
        if (_isBusy)
          const Padding(
            padding: EdgeInsets.only(top: 4),
            child: SizedBox(
              width: 20,
              height: 20,
              child: CircularProgressIndicator(
                strokeWidth: 2,
                color: AppColors.primary,
              ),
            ),
          )
        else if (widget.user.hasAvatar)
          TextButton(
            onPressed: _remove,
            style: TextButton.styleFrom(foregroundColor: AppColors.error),
            child: const Text('Fotoğrafı kaldır'),
          ),
      ],
    );
  }
}

// --- Pop-up'lar ------------------------------------------------------------

Future<void> _showNameDialog(
  BuildContext context,
  WidgetRef ref,
  UserSummary user,
) async {
  final controller = TextEditingController(text: user.name);

  await showDialog<void>(
    context: context,
    builder: (context) => _FormDialog(
      title: 'İsmini değiştir',
      submitLabel: 'Kaydet',
      fields: () => [
        UnderlinedTextField(
          label: 'İsim',
          controller: controller,
          textInputAction: TextInputAction.done,
          validator: (value) =>
              (value == null || value.trim().isEmpty) ? 'İsim gerekli' : null,
        ),
      ],
      onSubmit: () async {
        await ref
            .read(sereneApiProvider)
            .updateMe(name: controller.text.trim());
        ref.invalidate(profileProvider);
        return 'İsmin güncellendi.';
      },
    ),
  );
}

Future<void> _showEmailDialog(BuildContext context, WidgetRef ref) async {
  final emailController = TextEditingController();
  final passwordController = TextEditingController();

  final requested = await showDialog<bool>(
    context: context,
    builder: (context) => _FormDialog(
      title: 'E-postanı değiştir',
      description: 'Yeni adresine bir doğrulama kodu göndereceğiz.',
      submitLabel: 'Kod gönder',
      fields: () => [
        UnderlinedTextField(
          label: 'Yeni e-posta',
          controller: emailController,
          keyboardType: TextInputType.emailAddress,
          validator: validateEmail,
        ),
        const SizedBox(height: 16),
        UnderlinedTextField(
          label: 'Mevcut parolan',
          controller: passwordController,
          obscureText: true,
          textInputAction: TextInputAction.done,
          validator: (value) =>
              (value == null || value.isEmpty) ? 'Parola gerekli' : null,
        ),
      ],
      onSubmit: () async {
        await ref.read(sereneApiProvider).requestEmailChange(
              newEmail: emailController.text.trim(),
              currentPassword: passwordController.text,
            );
        return 'Kod ${emailController.text.trim()} adresine gönderildi.';
      },
      resultOnSuccess: true,
    ),
  );

  if (requested != true || !context.mounted) return;

  // Kod adımı ayrı bir pop-up: kullanıcı e-postasına bakıp geri dönebilsin.
  final codeController = TextEditingController();

  await showDialog<void>(
    context: context,
    builder: (context) => _FormDialog(
      title: 'Yeni adresini doğrula',
      description: 'Yeni e-posta adresine gönderdiğimiz 6 haneli kodu gir.',
      submitLabel: 'Onayla',
      fields: () => [
        UnderlinedTextField(
          label: 'Doğrulama kodu',
          controller: codeController,
          keyboardType: TextInputType.number,
          textInputAction: TextInputAction.done,
          validator: (value) => (value == null || value.trim().length != 6)
              ? '6 haneli kodu gir'
              : null,
        ),
      ],
      onSubmit: () async {
        await ref
            .read(sereneApiProvider)
            .confirmEmailChange(codeController.text.trim());
        ref.invalidate(profileProvider);
        return 'E-posta adresin güncellendi.';
      },
    ),
  );
}

Future<void> _showPasswordDialog(BuildContext context, WidgetRef ref) async {
  final currentController = TextEditingController();
  final newController = TextEditingController();
  final repeatController = TextEditingController();

  await showDialog<void>(
    context: context,
    builder: (context) => _FormDialog(
      title: 'Parolanı değiştir',
      submitLabel: 'Kaydet',
      fields: () => [
        UnderlinedTextField(
          label: 'Mevcut parolan',
          controller: currentController,
          obscureText: true,
          validator: (value) =>
              (value == null || value.isEmpty) ? 'Parola gerekli' : null,
        ),
        const SizedBox(height: 16),
        UnderlinedTextField(
          label: 'Yeni parolan',
          controller: newController,
          obscureText: true,
          validator: validatePassword,
        ),
        const SizedBox(height: 16),
        UnderlinedTextField(
          label: 'Yeni parolan (tekrar)',
          controller: repeatController,
          obscureText: true,
          textInputAction: TextInputAction.done,
          validator: (value) =>
              value != newController.text ? 'Parolalar eşleşmiyor' : null,
        ),
      ],
      onSubmit: () async {
        await ref.read(sereneApiProvider).changePassword(
              currentPassword: currentController.text,
              newPassword: newController.text,
            );
        return 'Parolan güncellendi.';
      },
    ),
  );
}

/// Doğrulama, yükleme durumu ve hata gösterimini tek yerde toplayan pop-up.
/// [onSubmit] başarılıysa döndürdüğü metin snackbar'da gösterilir.
class _FormDialog<T> extends StatefulWidget {
  const _FormDialog({
    required this.title,
    required this.submitLabel,
    required this.fields,
    required this.onSubmit,
    this.description,
    this.resultOnSuccess,
  });

  final String title;
  final String? description;
  final String submitLabel;
  final List<Widget> Function() fields;
  final Future<String> Function() onSubmit;
  final T? resultOnSuccess;

  @override
  State<_FormDialog<T>> createState() => _FormDialogState<T>();
}

class _FormDialogState<T> extends State<_FormDialog<T>> {
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
  Widget build(BuildContext context) => AlertDialog(
        backgroundColor: AppColors.surfaceContainerLowest,
        title: Text(
          widget.title,
          style:
              context.text.headlineSmall?.copyWith(color: AppColors.primary),
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
              backgroundColor: AppColors.primary,
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

// --- Ortak parçalar --------------------------------------------------------

class _EditRow extends StatelessWidget {
  const _EditRow({
    required this.icon,
    required this.label,
    required this.value,
    required this.onTap,
  });

  final IconData icon;
  final String label;
  final String value;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.symmetric(vertical: 4),
          child: Row(
            children: [
              CircleIcon(icon),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      label,
                      style: context.text.bodySmall
                          ?.copyWith(color: AppColors.onSurfaceVariant),
                    ),
                    Text(
                      value,
                      overflow: TextOverflow.ellipsis,
                      style: context.text.bodyLarge
                          ?.copyWith(color: AppColors.onSurface),
                    ),
                  ],
                ),
              ),
              const Icon(Icons.edit_outlined, color: AppColors.outline),
            ],
          ),
        ),
      );
}
