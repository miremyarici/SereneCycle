import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../../core/api/api_client.dart';
import '../../../core/api/models.dart';
import '../../../core/providers/app_providers.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/widgets/async_view.dart';
import '../../../core/widgets/pill_button.dart';
import '../../../core/widgets/soft_shadow_card.dart';

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
      appBar: AppBar(
        title: const Text(
          'Adet Döngüsü',
          style: TextStyle(
            fontSize: 20,
            fontWeight: FontWeight.w600,
            color: AppColors.primary,
          ),
        ),
        centerTitle: true,
      ),
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
    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);

    final picked = await showDatePicker(
      context: context,
      initialDate: _periodStart ?? today,
      // Gelecek tarih backend'de reddediliyor; bir yıldan eskisi de
      // tahmin için anlamlı değil.
      firstDate: DateTime(today.year - 1, today.month, today.day),
      lastDate: today,
      helpText: 'Son adet başlangıcı',
      confirmText: 'Seç',
      cancelText: 'Vazgeç',
    );

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

      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Döngü ayarların güncellendi.')),
      );
      context.pop();
    } on ApiException catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(e.message),
            backgroundColor: AppColors.error,
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _isSaving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final dateFormat = DateFormat('d MMMM yyyy', 'tr');

    return ListView(
      padding: const EdgeInsets.fromLTRB(24, 16, 24, 32),
      children: [
        SoftShadowCard(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const _CardTitle('DÖNGÜ UZUNLUĞU'),
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
              const _CardTitle('SON ADET BAŞLANGICI'),
              const SizedBox(height: 16),
              InkWell(
                onTap: _isSaving ? null : _pickPeriodStart,
                borderRadius: BorderRadius.circular(12),
                child: Padding(
                  padding: const EdgeInsets.symmetric(vertical: 4),
                  child: Row(
                    children: [
                      const _RowIcon(Icons.edit_calendar_outlined),
                      const SizedBox(width: 12),
                      Expanded(
                        child: Text(
                          _periodStart == null
                              ? 'Tarih seç'
                              : dateFormat.format(_periodStart!),
                          style: const TextStyle(
                            fontSize: 16,
                            fontWeight: FontWeight.w500,
                            color: AppColors.onSurface,
                          ),
                        ),
                      ),
                      const Icon(
                        Icons.chevron_right,
                        color: AppColors.outline,
                      ),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 12),
              const Text(
                'Tarihi değiştirmek yeni bir döngü kaydı başlatır ve '
                'tahminler buna göre yeniden hesaplanır.',
                style: TextStyle(
                  fontSize: 13,
                  height: 18 / 13,
                  color: AppColors.onSurfaceVariant,
                ),
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
          _RowIcon(icon),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  label,
                  style: const TextStyle(
                    fontSize: 16,
                    color: AppColors.onSurface,
                  ),
                ),
                Text(
                  helperText,
                  style: const TextStyle(
                    fontSize: 12,
                    color: AppColors.onSurfaceVariant,
                  ),
                ),
              ],
            ),
          ),
          _StepButton(
            icon: Icons.remove,
            tooltip: 'Azalt',
            onPressed: value > min ? () => onChanged(value - 1) : null,
          ),
          SizedBox(
            width: 64,
            child: Text(
              '$value gün',
              textAlign: TextAlign.center,
              style: const TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.w600,
                color: AppColors.primary,
              ),
            ),
          ),
          _StepButton(
            icon: Icons.add,
            tooltip: 'Artır',
            onPressed: value < max ? () => onChanged(value + 1) : null,
          ),
        ],
      );
}

class _StepButton extends StatelessWidget {
  const _StepButton({
    required this.icon,
    required this.tooltip,
    required this.onPressed,
  });

  final IconData icon;
  final String tooltip;
  final VoidCallback? onPressed;

  @override
  Widget build(BuildContext context) => IconButton(
        onPressed: onPressed,
        tooltip: tooltip,
        visualDensity: VisualDensity.compact,
        icon: Icon(icon, size: 20),
        style: IconButton.styleFrom(
          backgroundColor: AppColors.surfaceContainer,
          foregroundColor: AppColors.primary,
          disabledForegroundColor: AppColors.outline,
          shape: const CircleBorder(),
        ),
      );
}

class _RowIcon extends StatelessWidget {
  const _RowIcon(this.icon);

  final IconData icon;

  @override
  Widget build(BuildContext context) => Container(
        width: 40,
        height: 40,
        decoration: const BoxDecoration(
          color: AppColors.surfaceContainer,
          shape: BoxShape.circle,
        ),
        child: Icon(icon, size: 20, color: AppColors.secondary),
      );
}

class _CardTitle extends StatelessWidget {
  const _CardTitle(this.text);

  final String text;

  @override
  Widget build(BuildContext context) => Text(
        text,
        style: const TextStyle(
          fontSize: 14,
          fontWeight: FontWeight.w600,
          letterSpacing: 1.2,
          color: AppColors.secondary,
        ),
      );
}
