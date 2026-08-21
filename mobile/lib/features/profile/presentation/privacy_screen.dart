import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/api/api_client.dart';
import '../../../core/api/models.dart';
import '../../../core/providers/app_providers.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_typography.dart';
import '../../../core/widgets/app_snack_bar.dart';
import '../../../core/widgets/circle_icon.dart';
import '../../../core/widgets/form_dialog.dart';
import '../../../core/widgets/pill_button.dart';
import '../../../core/widgets/section_title.dart';
import '../../../core/widgets/soft_shadow_card.dart';
import '../../../core/widgets/underlined_text_field.dart';

/// Profil → "Verilerin ve hesabın". KVKK'nın iki hakkının uygulamadaki
/// karşılığı: veriyi dışa aktarmak ve hesabı sildirmek. Menstrüel veri özel
/// nitelikli kişisel veri olduğu için ikisi de "yakında" bırakılamaz.
class PrivacyScreen extends ConsumerWidget {
  const PrivacyScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) => Scaffold(
        appBar: AppBar(
          title: const Text('Verilerin ve Hesabın'),
          centerTitle: true,
        ),
        body: ListView(
          padding: const EdgeInsets.fromLTRB(24, 16, 24, 32),
          children: const [
            _Intro(),
            SizedBox(height: 20),
            _ExportCard(),
            SizedBox(height: 20),
            _DeleteAccountCard(),
          ],
        ),
      );
}

class _Intro extends StatelessWidget {
  const _Intro();

  @override
  Widget build(BuildContext context) => Text(
        'Adet kayıtların KVKK kapsamında özel nitelikli kişisel veridir. '
        'Bu verilerin bir kopyasını istediğin zaman alabilir, hesabını da '
        'kalıcı olarak sildirebilirsin.',
        style: context.text.bodyMedium
            ?.copyWith(color: AppColors.onSurfaceVariant),
      );
}

// --- Veri dışa aktarma -----------------------------------------------------

class _ExportCard extends ConsumerStatefulWidget {
  const _ExportCard();

  @override
  ConsumerState<_ExportCard> createState() => _ExportCardState();
}

class _ExportCardState extends ConsumerState<_ExportCard> {
  UserDataExport? _export;
  bool _isPreparing = false;

  Future<void> _prepare() async {
    setState(() => _isPreparing = true);

    try {
      final export = await ref.read(sereneApiProvider).exportMyData();
      if (mounted) setState(() => _export = export);
    } on ApiException catch (e) {
      if (mounted) context.showError(e.message);
    } finally {
      if (mounted) setState(() => _isPreparing = false);
    }
  }

  Future<void> _copy() async {
    final export = _export;
    if (export == null) return;

    await Clipboard.setData(ClipboardData(text: export.toPrettyJson()));

    if (mounted) {
      context.showMessage('Verilerin panoya kopyalandı: ${export.fileName}');
    }
  }

  @override
  Widget build(BuildContext context) {
    final export = _export;

    return SoftShadowCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const SectionTitle('VERİLERİNİ DIŞA AKTAR'),
          const SizedBox(height: 12),
          Text(
            'Profilin, bütün döngülerin, gün kayıtların ve öneri motorunun '
            'senin hakkında öğrendikleri tek bir JSON belgesinde toplanır.',
            style: context.text.bodyMedium
                ?.copyWith(color: AppColors.onSurfaceVariant),
          ),
          const SizedBox(height: 16),
          if (export == null)
            PillButton(
              label: 'Verilerimi hazırla',
              filled: true,
              isLoading: _isPreparing,
              onPressed: _prepare,
            )
          else
            _ExportSummary(export: export, onCopy: _copy, onRefresh: _prepare),
        ],
      ),
    );
  }
}

/// Hazırlanan belgenin özeti. Kullanıcı dosyayı açmadan ne indirdiğini
/// görebilmeli; ham JSON'u da altında açabiliyor.
class _ExportSummary extends StatefulWidget {
  const _ExportSummary({
    required this.export,
    required this.onCopy,
    required this.onRefresh,
  });

  final UserDataExport export;
  final VoidCallback onCopy;
  final VoidCallback onRefresh;

  @override
  State<_ExportSummary> createState() => _ExportSummaryState();
}

class _ExportSummaryState extends State<_ExportSummary> {
  /// Ham belge varsayılan olarak kapalı: özet çoğu kullanıcıya yeter,
  /// binlerce satırlık JSON ekranı boğar.
  bool _isDocumentVisible = false;

  @override
  Widget build(BuildContext context) => Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          _SummaryRow(
            icon: Icons.autorenew,
            label: 'Döngü kaydı',
            value: '${widget.export.cycleCount}',
          ),
          const SizedBox(height: 12),
          _SummaryRow(
            icon: Icons.event_note_outlined,
            label: 'Gün kaydı',
            value: '${widget.export.dailyLogCount}',
          ),
          const SizedBox(height: 16),
          PillButton(
            label: 'Panoya kopyala',
            filled: true,
            onPressed: widget.onCopy,
          ),
          const SizedBox(height: 4),
          // Yan yana koymak dar telefonlarda taşıyor; alt alta güvenli.
          TextButton(
            onPressed: () => setState(
              () => _isDocumentVisible = !_isDocumentVisible,
            ),
            style: TextButton.styleFrom(
              foregroundColor: AppColors.onSurfaceVariant,
            ),
            child: Text(
              _isDocumentVisible ? 'Belgeyi gizle' : 'Belgeyi görüntüle',
            ),
          ),
          TextButton(
            onPressed: widget.onRefresh,
            style: TextButton.styleFrom(foregroundColor: AppColors.primary),
            child: const Text('Yeniden hazırla'),
          ),
          if (_isDocumentVisible) ...[
            const SizedBox(height: 8),
            Container(
              width: double.infinity,
              constraints: const BoxConstraints(maxHeight: 260),
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: AppColors.surfaceContainerLow,
                borderRadius: BorderRadius.circular(12),
              ),
              child: SingleChildScrollView(
                child: SelectableText(
                  widget.export.toPrettyJson(),
                  style: context.text.bodySmall?.copyWith(
                    fontFamily: 'monospace',
                    color: AppColors.onSurface,
                  ),
                ),
              ),
            ),
          ],
        ],
      );
}

class _SummaryRow extends StatelessWidget {
  const _SummaryRow({
    required this.icon,
    required this.label,
    required this.value,
  });

  final IconData icon;
  final String label;
  final String value;

  @override
  Widget build(BuildContext context) => Row(
        children: [
          CircleIcon(icon),
          const SizedBox(width: 12),
          Expanded(
            child: Text(
              label,
              style:
                  context.text.bodyLarge?.copyWith(color: AppColors.onSurface),
            ),
          ),
          Text(
            value,
            style: context.text.bodyLarge?.copyWith(
              fontWeight: FontWeight.w600,
              color: AppColors.primary,
            ),
          ),
        ],
      );
}

// --- Hesap silme -----------------------------------------------------------

class _DeleteAccountCard extends ConsumerWidget {
  const _DeleteAccountCard();

  @override
  Widget build(BuildContext context, WidgetRef ref) => SoftShadowCard(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const SectionTitle('HESABINI SİL'),
            const SizedBox(height: 12),
            Text(
              'Hesabın, bütün döngü ve gün kayıtların, öğrenilmiş tercihlerin '
              've profil fotoğrafın sunucudan kalıcı olarak silinir. '
              'Bu işlem geri alınamaz.',
              style: context.text.bodyMedium
                  ?.copyWith(color: AppColors.onSurfaceVariant),
            ),
            const SizedBox(height: 16),
            OutlinedButton.icon(
              onPressed: () => _confirmDelete(context, ref),
              icon: const Icon(Icons.delete_outline),
              label: const Text('Hesabımı kalıcı olarak sil'),
              style: OutlinedButton.styleFrom(
                foregroundColor: AppColors.error,
                side: const BorderSide(color: AppColors.error),
                shape: const StadiumBorder(),
                padding: const EdgeInsets.symmetric(vertical: 14),
              ),
            ),
          ],
        ),
      );
}

/// Silmeden önce parola sorulur: geri alınamaz bir işlem için açık oturum
/// yetmez, telefonu eline geçiren biri hesabı yok edememeli.
Future<void> _confirmDelete(BuildContext context, WidgetRef ref) async {
  final passwordController = TextEditingController();

  await showDialog<void>(
    context: context,
    builder: (context) => FormDialog(
      title: 'Hesabını sil',
      description: 'Bu işlem geri alınamaz. Devam etmek için parolanı gir.',
      submitLabel: 'Kalıcı olarak sil',
      isDestructive: true,
      fields: () => [
        UnderlinedTextField(
          label: 'Parolan',
          controller: passwordController,
          obscureText: true,
          textInputAction: TextInputAction.done,
          validator: (value) =>
              (value == null || value.isEmpty) ? 'Parola gerekli' : null,
        ),
      ],
      onSubmit: () async {
        // Oturum düşünce giriş ekranına dönüşü router üstleniyor.
        await ref
            .read(authControllerProvider.notifier)
            .deleteAccount(passwordController.text);

        return 'Hesabın ve bütün verilerin silindi.';
      },
    ),
  );
}
