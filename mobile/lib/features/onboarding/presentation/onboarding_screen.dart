import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../../core/api/api_client.dart';
import '../../../core/providers/app_providers.dart';
import '../../../core/router/route_paths.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/widgets/pill_button.dart';
import '../../../core/widgets/soft_shadow_card.dart';

/// Doğrulamadan sonraki 3 soruluk sihirbaz: son adet başlangıcı, ortalama
/// döngü uzunluğu, adet süresi. `/phase/today` bu bilgi olmadan 404 döner,
/// bu yüzden bottom nav'a girmeden önce mecburi.
class OnboardingScreen extends ConsumerStatefulWidget {
  const OnboardingScreen({super.key});

  @override
  ConsumerState<OnboardingScreen> createState() => _OnboardingScreenState();
}

class _OnboardingScreenState extends ConsumerState<OnboardingScreen> {
  static const _totalSteps = 3;

  // Backend'in UpdateProfileRequest'teki [Range] sınırlarıyla eşleşir.
  static const _minCycleLength = 21;
  static const _maxCycleLength = 45;
  static const _minPeriodLength = 1;
  static const _maxPeriodLength = 14;

  int _step = 1;
  DateTime? _lastPeriodStart;
  int _cycleLength = 28;
  int _periodLength = 5;
  bool _isSubmitting = false;

  bool get _canContinue => _step != 1 || _lastPeriodStart != null;

  Future<void> _pickPeriodStart() async {
    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);

    final picked = await showDatePicker(
      context: context,
      initialDate: _lastPeriodStart ?? today,
      firstDate: DateTime(today.year - 1, today.month, today.day),
      lastDate: today,
      helpText: 'Son adet başlangıcı',
      confirmText: 'Seç',
      cancelText: 'Vazgeç',
    );

    if (picked != null) {
      setState(() => _lastPeriodStart = picked);
    }
  }

  void _back() {
    if (_step > 1) setState(() => _step--);
  }

  Future<void> _next() async {
    if (!_canContinue) return;

    if (_step < _totalSteps) {
      setState(() => _step++);
      return;
    }

    setState(() => _isSubmitting = true);

    try {
      await ref.read(sereneApiProvider).updateMe(
            avgCycleLength: _cycleLength,
            avgPeriodLength: _periodLength,
            lastPeriodStart: _lastPeriodStart,
          );

      ref.invalidate(profileProvider);
      ref.invalidate(phaseTodayProvider);
      ref.invalidate(nutritionProvider);
      ref.invalidate(exerciseProvider);

      if (mounted) context.go(RoutePaths.home);
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
      if (mounted) setState(() => _isSubmitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: Column(
          children: [
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
              child: Row(
                children: [
                  SizedBox(
                    width: 40,
                    child: _step > 1
                        ? IconButton(
                            icon: const Icon(Icons.arrow_back),
                            color: AppColors.primary,
                            onPressed: _isSubmitting ? null : _back,
                          )
                        : null,
                  ),
                  Expanded(
                    child: Row(
                      children: List.generate(_totalSteps, (index) {
                        final active = index < _step;
                        return Expanded(
                          child: Container(
                            margin: EdgeInsets.only(
                              right: index == _totalSteps - 1 ? 0 : 8,
                            ),
                            height: 6,
                            decoration: BoxDecoration(
                              color: active
                                  ? AppColors.primaryContainer
                                  : AppColors.surfaceContainerHighest,
                              borderRadius: BorderRadius.circular(4),
                            ),
                          ),
                        );
                      }),
                    ),
                  ),
                  const SizedBox(width: 40),
                ],
              ),
            ),
            Expanded(
              child: Center(
                child: SingleChildScrollView(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
                  child: ConstrainedBox(
                    constraints: const BoxConstraints(maxWidth: 448),
                    child: SoftShadowCard(
                      padding: const EdgeInsets.all(24),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          _buildStep(),
                          const SizedBox(height: 24),
                          PillButton(
                            label: _step == _totalSteps ? 'Başla' : 'Devam Et',
                            filled: true,
                            isLoading: _isSubmitting,
                            onPressed: _canContinue ? _next : null,
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildStep() {
    switch (_step) {
      case 1:
        return _StepContent(
          title: 'Son adet başlangıcın ne zaman?',
          subtitle:
              'Bu tarih, döngünü ve fazını tahmin etmemiz için başlangıç '
              'noktası olacak.',
          child: _PeriodStartPicker(
            value: _lastPeriodStart,
            onTap: _isSubmitting ? null : _pickPeriodStart,
          ),
        );
      case 2:
        return _StepContent(
          title: 'Ortalama döngü uzunluğun kaç gün?',
          subtitle: 'Emin değilsen varsayılan 28 günle devam edebilirsin, '
              'sonra düzeltebilirsin.',
          child: _Stepper(
            value: _cycleLength,
            min: _minCycleLength,
            max: _maxCycleLength,
            onChanged: (value) => setState(() => _cycleLength = value),
          ),
        );
      default:
        return _StepContent(
          title: 'Adetin genelde kaç gün sürüyor?',
          subtitle: 'Bu bilgiyi istediğin zaman profilinden değiştirebilirsin.',
          child: _Stepper(
            value: _periodLength,
            min: _minPeriodLength,
            max: _maxPeriodLength,
            onChanged: (value) => setState(() => _periodLength = value),
          ),
        );
    }
  }
}

class _StepContent extends StatelessWidget {
  const _StepContent({
    required this.title,
    required this.subtitle,
    required this.child,
  });

  final String title;
  final String subtitle;
  final Widget child;

  @override
  Widget build(BuildContext context) => Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(
            title,
            style: const TextStyle(
              fontSize: 24,
              height: 32 / 24,
              fontWeight: FontWeight.w600,
              color: AppColors.onSurface,
            ),
          ),
          const SizedBox(height: 8),
          Text(
            subtitle,
            style: const TextStyle(
              fontSize: 16,
              height: 24 / 16,
              color: AppColors.onSurfaceVariant,
            ),
          ),
          const SizedBox(height: 40),
          child,
        ],
      );
}

class _PeriodStartPicker extends StatelessWidget {
  const _PeriodStartPicker({required this.value, required this.onTap});

  final DateTime? value;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final dateFormat = DateFormat('d MMMM yyyy', 'tr');

    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(12),
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
        decoration: BoxDecoration(
          color: AppColors.surfaceContainerLow,
          borderRadius: BorderRadius.circular(16),
        ),
        child: Row(
          children: [
            const Icon(
              Icons.edit_calendar_outlined,
              color: AppColors.primary,
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Text(
                value == null ? 'Tarih seç' : dateFormat.format(value!),
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.w500,
                  color: value == null
                      ? AppColors.onSurfaceVariant
                      : AppColors.onSurface,
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

class _Stepper extends StatelessWidget {
  const _Stepper({
    required this.value,
    required this.min,
    required this.max,
    required this.onChanged,
  });

  final int value;
  final int min;
  final int max;
  final ValueChanged<int> onChanged;

  @override
  Widget build(BuildContext context) => Row(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          _StepButton(
            icon: Icons.remove,
            tooltip: 'Azalt',
            onPressed: value > min ? () => onChanged(value - 1) : null,
          ),
          SizedBox(
            width: 96,
            child: Column(
              children: [
                Text(
                  '$value',
                  textAlign: TextAlign.center,
                  style: const TextStyle(
                    fontSize: 32,
                    fontWeight: FontWeight.w600,
                    color: AppColors.primary,
                  ),
                ),
                const Text(
                  'gün',
                  style: TextStyle(
                    fontSize: 12,
                    color: AppColors.onSurfaceVariant,
                  ),
                ),
              ],
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
        icon: Icon(icon, size: 24),
        style: IconButton.styleFrom(
          backgroundColor: AppColors.surfaceContainer,
          foregroundColor: AppColors.primary,
          disabledForegroundColor: AppColors.outline,
          minimumSize: const Size(56, 56),
          shape: const CircleBorder(),
        ),
      );
}
