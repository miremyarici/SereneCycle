import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../../core/api/api_client.dart';
import '../../../core/api/models.dart';
import '../../../core/providers/app_providers.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_typography.dart';
import '../../../core/theme/blood_colors.dart';
import '../../../core/widgets/app_snack_bar.dart';
import '../../../core/widgets/async_view.dart';
import '../../../core/widgets/pill_button.dart';
import '../../../core/widgets/section_title.dart';
import '../../../core/widgets/soft_shadow_card.dart';

/// Takvimde bir güne dokununca açılır: o günün kanama, kan rengi,
/// lekelenme ve belirti kaydı.
class PeriodLogScreen extends ConsumerWidget {
  const PeriodLogScreen({required this.date, super.key});

  final DateTime date;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final day = DateTime(date.year, date.month, date.day);
    final log = ref.watch(dailyLogProvider(day));
    final symptoms = ref.watch(symptomOptionsProvider);

    return Scaffold(
      appBar: AppBar(
        title: Text(DateFormat('d MMMM yyyy', 'tr').format(day)),
        centerTitle: true,
      ),
      // İki istek de gerekli: form başlangıç değerlerini bir kez okuduğu
      // için ikisi de gelmeden formu kurmuyoruz.
      body: AsyncView(
        value: log,
        onRetry: () => ref.invalidate(dailyLogProvider(day)),
        builder: (entry) => AsyncView(
          value: symptoms,
          onRetry: () => ref.invalidate(symptomOptionsProvider),
          builder: (options) => _LogForm(
            date: day,
            entry: entry,
            symptomOptions: options,
          ),
        ),
      ),
    );
  }
}

class _LogForm extends ConsumerStatefulWidget {
  const _LogForm({
    required this.date,
    required this.entry,
    required this.symptomOptions,
  });

  final DateTime date;
  final DailyLogEntry entry;
  final List<SymptomOption> symptomOptions;

  @override
  ConsumerState<_LogForm> createState() => _LogFormState();
}

class _LogFormState extends ConsumerState<_LogForm> {
  late bool _hasBleeding = widget.entry.hasBleeding;
  late FlowLevel? _flow = widget.entry.flow;
  late BloodColorOption? _bloodColor = widget.entry.bloodColor;
  late bool _hasSpotting = widget.entry.hasSpotting;
  late final Set<int> _symptomIds = {...widget.entry.symptomIds};
  bool _isSaving = false;

  Future<void> _save() async {
    setState(() => _isSaving = true);

    try {
      await ref.read(sereneApiProvider).saveDailyLog(
            date: widget.date,
            hasBleeding: _hasBleeding,
            hasSpotting: _hasSpotting,
            flow: _hasBleeding ? _flow : null,
            bloodColor: _hasBleeding ? _bloodColor : null,
            symptomIds: _symptomIds,
          );

      // Kanama kaydı yeni bir döngü başlatmış olabilir: faza bağlı her şey
      // yeniden çekilsin.
      ref.invalidate(dailyLogProvider(widget.date));
      ref.invalidate(calendarMonthProvider);
      ref.invalidate(phaseTodayProvider);
      ref.invalidate(profileProvider);
      ref.invalidate(nutritionProvider);
      ref.invalidate(exerciseProvider);

      if (!mounted) return;

      context.showMessage('Gün kaydedildi.');
      context.pop();
    } on ApiException catch (e) {
      if (mounted) context.showError(e.message);
    } finally {
      if (mounted) setState(() => _isSaving = false);
    }
  }

  void _toggleSymptom(int id) {
    setState(() {
      if (!_symptomIds.remove(id)) _symptomIds.add(id);
    });
  }

  @override
  Widget build(BuildContext context) => ListView(
        padding: const EdgeInsets.fromLTRB(24, 16, 24, 32),
        children: [
          SoftShadowCard(
            child: _BleedingSection(
              hasBleeding: _hasBleeding,
              flow: _flow,
              bloodColor: _bloodColor,
              isEnabled: !_isSaving,
              onBleedingChanged: (value) =>
                  setState(() => _hasBleeding = value),
              // Aynı seçeneğe tekrar dokunmak seçimi kaldırır.
              onFlowTap: (level) =>
                  setState(() => _flow = _flow == level ? null : level),
              onBloodColorTap: (option) => setState(
                () => _bloodColor = _bloodColor == option ? null : option,
              ),
            ),
          ),
          const SizedBox(height: 20),
          SoftShadowCard(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const SectionTitle('LEKELENME'),
                const SizedBox(height: 8),
                _SwitchRow(
                  label: 'Lekelenme var',
                  helperText: 'Takvimde damlanın altında ayrı bir nokta '
                      'olarak görünür.',
                  value: _hasSpotting,
                  onChanged: _isSaving
                      ? null
                      : (value) => setState(() => _hasSpotting = value),
                ),
              ],
            ),
          ),
          const SizedBox(height: 20),
          SoftShadowCard(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const SectionTitle('BELİRTİLER'),
                const SizedBox(height: 16),
                _ChipRow(
                  children: [
                    for (final option in widget.symptomOptions)
                      _SelectableChip(
                        label: option.name,
                        selected: _symptomIds.contains(option.id),
                        onTap:
                            _isSaving ? null : () => _toggleSymptom(option.id),
                      ),
                  ],
                ),
              ],
            ),
          ),
          const SizedBox(height: 32),
          PillButton(
            label: 'Kaydet',
            filled: true,
            isLoading: _isSaving,
            onPressed: _save,
          ),
        ],
      );
}

/// Kanama anahtarı ve —açıksa— düzey/renk seçimleri.
class _BleedingSection extends StatelessWidget {
  const _BleedingSection({
    required this.hasBleeding,
    required this.flow,
    required this.bloodColor,
    required this.isEnabled,
    required this.onBleedingChanged,
    required this.onFlowTap,
    required this.onBloodColorTap,
  });

  final bool hasBleeding;
  final FlowLevel? flow;
  final BloodColorOption? bloodColor;
  final bool isEnabled;
  final ValueChanged<bool> onBleedingChanged;
  final ValueChanged<FlowLevel> onFlowTap;
  final ValueChanged<BloodColorOption> onBloodColorTap;

  @override
  Widget build(BuildContext context) => Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const SectionTitle('KANAMA'),
          const SizedBox(height: 8),
          _SwitchRow(
            label: 'Kanama var',
            value: hasBleeding,
            onChanged: isEnabled ? onBleedingChanged : null,
          ),
          // Şiddet ve renk yalnızca kanama varken sorulur.
          if (hasBleeding) ...[
            const Divider(height: 24, color: AppColors.outlineVariant),
            const _FieldLabel('Kanama düzeyi'),
            const SizedBox(height: 12),
            _ChipRow(
              children: [
                for (final level in FlowLevel.values)
                  _SelectableChip(
                    label: level.label,
                    selected: flow == level,
                    onTap: isEnabled ? () => onFlowTap(level) : null,
                  ),
              ],
            ),
            const SizedBox(height: 24),
            const _FieldLabel('Kan rengi'),
            const SizedBox(height: 12),
            _ChipRow(
              children: [
                for (final option in BloodColorOption.values)
                  _SelectableChip(
                    label: option.label,
                    selected: bloodColor == option,
                    swatch: option.swatch,
                    onTap: isEnabled ? () => onBloodColorTap(option) : null,
                  ),
              ],
            ),
          ],
        ],
      );
}

/// Seçim çipleri her yerde aynı boşlukla sarmalansın diye.
class _ChipRow extends StatelessWidget {
  const _ChipRow({required this.children});

  static const _spacing = 12.0;

  final List<Widget> children;

  @override
  Widget build(BuildContext context) => Wrap(
        spacing: _spacing,
        runSpacing: _spacing,
        children: children,
      );
}

/// Etiket + anahtar satırı. `SwitchListTile` yerine sade bir [Row]:
/// [SoftShadowCard] kendi zeminini boyadığı için ListTile'ın ink efekti
/// görünmez kalıyordu.
class _SwitchRow extends StatelessWidget {
  const _SwitchRow({
    required this.label,
    required this.value,
    required this.onChanged,
    this.helperText,
  });

  final String label;
  final bool value;
  final ValueChanged<bool>? onChanged;
  final String? helperText;

  @override
  Widget build(BuildContext context) => Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  label,
                  style: context.text.bodyLarge
                      ?.copyWith(color: AppColors.onSurface),
                ),
                if (helperText != null) ...[
                  const SizedBox(height: 2),
                  Text(
                    helperText!,
                    style: context.text.bodySmall
                        ?.copyWith(color: AppColors.onSurfaceVariant),
                  ),
                ],
              ],
            ),
          ),
          const SizedBox(width: 12),
          Switch.adaptive(
            value: value,
            onChanged: onChanged,
            activeThumbColor: AppColors.primary,
          ),
        ],
      );
}

/// Seçilebilir çip. Aynı bileşen hem tekli (kanama düzeyi, renk) hem çoklu
/// (belirtiler) seçimde kullanılıyor; fark yalnızca çağıranın mantığında.
class _SelectableChip extends StatelessWidget {
  const _SelectableChip({
    required this.label,
    required this.selected,
    required this.onTap,
    this.swatch,
  });

  static const _radius = 999.0;
  static const _swatchSize = 12.0;

  final String label;
  final bool selected;
  final VoidCallback? onTap;
  final Color? swatch;

  @override
  Widget build(BuildContext context) => InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(_radius),
        child: Container(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
          decoration: BoxDecoration(
            color: selected
                ? AppColors.primaryContainer
                : AppColors.surfaceContainerLowest,
            border: Border.all(
              color: selected ? AppColors.primary : AppColors.outlineVariant,
            ),
            borderRadius: BorderRadius.circular(_radius),
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              if (swatch != null) ...[
                Container(
                  width: _swatchSize,
                  height: _swatchSize,
                  decoration: BoxDecoration(
                    color: swatch,
                    shape: BoxShape.circle,
                    border: Border.all(color: AppColors.outlineVariant),
                  ),
                ),
                const SizedBox(width: 8),
              ],
              Text(
                label,
                style: context.text.labelLarge?.copyWith(
                  color: selected
                      ? AppColors.onPrimaryContainer
                      : AppColors.onSurface,
                ),
              ),
            ],
          ),
        ),
      );
}

class _FieldLabel extends StatelessWidget {
  const _FieldLabel(this.text);

  final String text;

  @override
  Widget build(BuildContext context) => Text(
        text,
        style: context.text.bodyLarge?.copyWith(
          fontWeight: FontWeight.w500,
          color: AppColors.onSurface,
        ),
      );
}
