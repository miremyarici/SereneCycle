import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../../core/api/api_client.dart';
import '../../../core/api/models.dart';
import '../../../core/providers/app_providers.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/blood_colors.dart';
import '../../../core/widgets/async_view.dart';
import '../../../core/widgets/pill_button.dart';
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
        title: Text(
          DateFormat('d MMMM yyyy', 'tr').format(day),
          style: const TextStyle(
            fontSize: 20,
            fontWeight: FontWeight.w600,
            color: AppColors.primary,
          ),
        ),
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

      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Gün kaydedildi.')),
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
  Widget build(BuildContext context) => ListView(
        padding: const EdgeInsets.fromLTRB(24, 16, 24, 32),
        children: [
          SoftShadowCard(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const _CardTitle('KANAMA'),
                const SizedBox(height: 8),
                _SwitchRow(
                  label: 'Kanama var',
                  value: _hasBleeding,
                  onChanged: _isSaving
                      ? null
                      : (value) => setState(() => _hasBleeding = value),
                ),
                // Şiddet ve renk yalnızca kanama varken sorulur.
                if (_hasBleeding) ...[
                  const Divider(height: 24, color: AppColors.outlineVariant),
                  const _FieldLabel('Kanama düzeyi'),
                  const SizedBox(height: 12),
                  Wrap(
                    spacing: 12,
                    runSpacing: 12,
                    children: FlowLevel.values
                        .map((level) => _ChoiceChip(
                              label: level.label,
                              selected: _flow == level,
                              onTap: _isSaving
                                  ? null
                                  : () => setState(
                                        () => _flow =
                                            _flow == level ? null : level,
                                      ),
                            ))
                        .toList(),
                  ),
                  const SizedBox(height: 24),
                  const _FieldLabel('Kan rengi'),
                  const SizedBox(height: 12),
                  Wrap(
                    spacing: 12,
                    runSpacing: 12,
                    children: BloodColorOption.values
                        .map((option) => _ChoiceChip(
                              label: option.label,
                              selected: _bloodColor == option,
                              swatch: option.swatch,
                              onTap: _isSaving
                                  ? null
                                  : () => setState(
                                        () => _bloodColor =
                                            _bloodColor == option
                                                ? null
                                                : option,
                                      ),
                            ))
                        .toList(),
                  ),
                ],
              ],
            ),
          ),
          const SizedBox(height: 20),
          SoftShadowCard(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const _CardTitle('LEKELENME'),
                const SizedBox(height: 8),
                _SwitchRow(
                  label: 'Lekelenme var',
                  helperText:
                      'Takvimde damlanın altında ayrı bir nokta olarak '
                      'görünür.',
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
                const _CardTitle('BELİRTİLER'),
                const SizedBox(height: 16),
                Wrap(
                  spacing: 12,
                  runSpacing: 12,
                  children: widget.symptomOptions
                      .map((option) => _ChoiceChip(
                            label: option.name,
                            selected: _symptomIds.contains(option.id),
                            onTap: _isSaving
                                ? null
                                : () => setState(() {
                                      if (!_symptomIds.remove(option.id)) {
                                        _symptomIds.add(option.id);
                                      }
                                    }),
                          ))
                      .toList(),
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
                  style: const TextStyle(
                    fontSize: 16,
                    color: AppColors.onSurface,
                  ),
                ),
                if (helperText != null) ...[
                  const SizedBox(height: 2),
                  Text(
                    helperText!,
                    style: const TextStyle(
                      fontSize: 12,
                      height: 16 / 12,
                      color: AppColors.onSurfaceVariant,
                    ),
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
class _ChoiceChip extends StatelessWidget {
  const _ChoiceChip({
    required this.label,
    required this.selected,
    required this.onTap,
    this.swatch,
  });

  final String label;
  final bool selected;
  final VoidCallback? onTap;
  final Color? swatch;

  @override
  Widget build(BuildContext context) => InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(999),
        child: Container(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
          decoration: BoxDecoration(
            color: selected
                ? AppColors.primaryContainer
                : AppColors.surfaceContainerLowest,
            border: Border.all(
              color: selected ? AppColors.primary : AppColors.outlineVariant,
            ),
            borderRadius: BorderRadius.circular(999),
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              if (swatch != null) ...[
                Container(
                  width: 12,
                  height: 12,
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
                style: TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.w600,
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
        style: const TextStyle(
          fontSize: 16,
          fontWeight: FontWeight.w500,
          color: AppColors.onSurface,
        ),
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
