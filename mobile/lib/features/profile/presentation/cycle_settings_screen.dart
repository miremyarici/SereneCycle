import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../../core/api/api_client.dart';
import '../../../core/api/models.dart';
import '../../../core/providers/app_providers.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_typography.dart';
import '../../../core/utils/period_start_picker.dart';
import '../../../core/widgets/app_snack_bar.dart';
import '../../../core/widgets/async_view.dart';
import '../../../core/widgets/circle_icon.dart';
import '../../../core/widgets/pill_button.dart';
import '../../../core/widgets/section_title.dart';
import '../../../core/widgets/soft_shadow_card.dart';
import '../../../core/widgets/stepper_button.dart';

/// Profil → "Adet döngünü düzenle". Faz tahminini besleyen üç değer burada
/// güncellenir: ortalama döngü uzunluğu, adet süresi ve son adet başlangıcı.
class CycleSettingsScreen extends ConsumerWidget {
  const CycleSettingsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final profile = ref.watch(profileProvider);

    // Son adet başlangıcı /me'de dönmüyor, faz yanıtından geliyor. Form
    // başlangıç değerini bir kez okuduğu için yanıtı beklemek gerekiyor;
    // hiç döngü kaydı yoksa istek hata döner ve alan boş başlar.
    final phase = ref.watch(phaseTodayProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Adet Döngüsü'), centerTitle: true),
      body: phase.isLoading
          ? const Center(
              child: CircularProgressIndicator(color: AppColors.primary),
            )
          : AsyncView(
              value: profile,
              onRetry: () => ref.invalidate(profileProvider),
              builder: (user) => _CycleForm(
                user: user,
                initialPeriodStart: phase.value?.cycleStartDate,
              ),
            ),
    );
  }
}

class _CycleForm extends ConsumerStatefulWidget {
  const _CycleForm({required this.user, required this.initialPeriodStart});

  final UserSummary user;
  final DateTime? initialPeriodStart;

  @override
  ConsumerState<_CycleForm> createState() => _CycleFormState();
}

class _CycleFormState extends ConsumerState<_CycleForm> {
  late int _cycleLength = widget.user.avgCycleLength;
  late int _periodLength = widget.user.avgPeriodLength;
  late DateTime? _periodStart = widget.initialPeriodStart;
  bool _isSaving = false;

  // Backend'in kabul ettiği aralıklar (UpdateProfileRequest).
  static const _minCycleLength = 21;
  static const _maxCycleLength = 45;
  static const _minPeriodLength = 1;
  static const _maxPeriodLength = 14;

  bool get _cycleLengthChanged => _cycleLength != widget.user.avgCycleLength;
  bool get _periodLengthChanged => _periodLength != widget.user.avgPeriodLength;
  bool get _periodStartChanged => _periodStart != widget.initialPeriodStart;

  bool get _hasChanges =>
      _cycleLengthChanged || _periodLengthChanged || _periodStartChanged;

  Future<void> _pickPeriodStart() async {
    final picked =
        await pickPeriodStartDate(context, initialDate: _periodStart);

    if (picked != null) {
      setState(() => _periodStart = picked);
    }
  }

  Future<void> _save() async {
    setState(() => _isSaving = true);

    try {
      // Yalnızca değişenler gönderilir: son adet tarihi gönderildiğinde
      // backend yeni bir döngü kaydı açıyor, dokunulmadıysa göndermeyelim.
      await ref.read(sereneApiProvider).updateMe(
            avgCycleLength: _cycleLengthChanged ? _cycleLength : null,
            avgPeriodLength: _periodLengthChanged ? _periodLength : null,
            lastPeriodStart: _periodStartChanged ? _periodStart : null,
          );

      // Faz, beslenme ve hareket içerikleri bu ayarlardan türüyor.
      ref.invalidate(profileProvider);
      ref.invalidate(phaseTodayProvider);
      ref.invalidate(nutritionProvider);
      ref.invalidate(exerciseProvider);

      if (!mounted) return;

      context.showMessage('Döngü ayarların güncellendi.');
      context.pop();
    } on ApiException catch (e) {
      if (mounted) context.showError(e.message);
    } finally {
      if (mounted) setState(() => _isSaving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.fromLTRB(24, 16, 24, 32),
      children: [
        SoftShadowCard(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const SectionTitle('DÖNGÜ UZUNLUĞU'),
              const SizedBox(height: 16),
              _StepperRow(
                icon: Icons.calendar_month_outlined,
                label: 'Ortalama döngü uzunluğu',
                helperText: 'Bir adetin ilk gününden diğerine kadar',
                value: _cycleLength,
                min: _minCycleLength,
                max: _maxCycleLength,
                onChanged: (value) => setState(() => _cycleLength = value),
              ),
              const Divider(height: 32, color: AppColors.outlineVariant),
              _StepperRow(
                icon: Icons.water_drop_outlined,
                label: 'Adet süresi',
                helperText: 'Kanamanın sürdüğü gün sayısı',
                value: _periodLength,
                min: _minPeriodLength,
                max: _maxPeriodLength,
                onChanged: (value) => setState(() => _periodLength = value),
              ),
            ],
          ),
        ),
        const SizedBox(height: 20),
        SoftShadowCard(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const SectionTitle('SON ADET BAŞLANGICI'),
              const SizedBox(height: 16),
              _PeriodStartRow(
                value: _periodStart,
                onTap: _isSaving ? null : _pickPeriodStart,
              ),
              const SizedBox(height: 12),
              Text(
                'Tarihi değiştirmek yeni bir döngü kaydı başlatır ve '
                'tahminler buna göre yeniden hesaplanır.',
                style: context.text.bodyMedium
                    ?.copyWith(color: AppColors.onSurfaceVariant),
              ),
            ],
          ),
        ),
        const SizedBox(height: 32),
        PillButton(
          label: 'Kaydet',
          filled: true,
          isLoading: _isSaving,
          onPressed: _hasChanges ? _save : null,
        ),
      ],
    );
  }
}

/// Son adet başlangıcını açan satır.
class _PeriodStartRow extends StatelessWidget {
  const _PeriodStartRow({required this.value, required this.onTap});

  final DateTime? value;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final date = value;

    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(12),
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 4),
        child: Row(
          children: [
            const CircleIcon(Icons.edit_calendar_outlined),
            const SizedBox(width: 12),
            Expanded(
              child: Text(
                date == null
                    ? 'Tarih seç'
                    : DateFormat('d MMMM yyyy', 'tr').format(date),
                style: context.text.bodyLarge?.copyWith(
                  fontWeight: FontWeight.w500,
                  color: AppColors.onSurface,
                ),
              ),
            ),
            const Icon(Icons.chevron_right, color: AppColors.outline),
          ],
        ),
      ),
    );
  }
}

class _StepperRow extends StatelessWidget {
  const _StepperRow({
    required this.icon,
    required this.label,
    required this.helperText,
    required this.value,
    required this.min,
    required this.max,
    required this.onChanged,
  });

  static const _valueWidth = 64.0;

  final IconData icon;
  final String label;
  final String helperText;
  final int value;
  final int min;
  final int max;
  final ValueChanged<int> onChanged;

  @override
  Widget build(BuildContext context) => Row(
        children: [
          CircleIcon(icon),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  label,
                  style: context.text.bodyLarge
                      ?.copyWith(color: AppColors.onSurface),
                ),
                Text(
                  helperText,
                  style: context.text.bodySmall
                      ?.copyWith(color: AppColors.onSurfaceVariant),
                ),
              ],
            ),
          ),
          StepperButton(
            icon: Icons.remove,
            tooltip: 'Azalt',
            onPressed: value > min ? () => onChanged(value - 1) : null,
          ),
          SizedBox(
            width: _valueWidth,
            child: Text(
              '$value gün',
              textAlign: TextAlign.center,
              style: context.text.bodyLarge?.copyWith(
                fontWeight: FontWeight.w600,
                color: AppColors.primary,
              ),
            ),
          ),
          StepperButton(
            icon: Icons.add,
            tooltip: 'Artır',
            onPressed: value < max ? () => onChanged(value + 1) : null,
          ),
        ],
      );
}
