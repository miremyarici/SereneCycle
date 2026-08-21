import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../../core/api/api_client.dart';
import '../../../core/api/models.dart';
import '../../../core/providers/app_providers.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_typography.dart';
import '../../../core/utils/period_start_picker.dart';
import '../../../core/widgets/app_snack_bar.dart';
import '../../../core/widgets/pill_button.dart';
import '../../../core/widgets/soft_shadow_card.dart';
import '../../../core/widgets/stepper_button.dart';

/// Doğrulamadan sonraki sihirbaz. İlk üç adım döngü tahmini için mecburi:
/// `/phase/today` bu bilgi olmadan 404 döner. Son iki adım öneri motorunu
/// besler ve bilinçli olarak atlanabilir — kısıt işaretlenmezse hiçbir şey
/// elenmez, zevk işaretlenmezse motor düzgün dağılımdan başlar.
///
/// İki anketin nereye gittiği ayrıdır: kısıtlar (alerji, sakatlık, ekipman)
/// sert filtreye, zevk cevapları öğrenmenin başlangıç noktasına yazılır.
class OnboardingScreen extends ConsumerStatefulWidget {
  const OnboardingScreen({super.key});

  @override
  ConsumerState<OnboardingScreen> createState() => _OnboardingScreenState();
}

class _OnboardingScreenState extends ConsumerState<OnboardingScreen> {
  static const _totalSteps = 5;
  static const _cardMaxWidth = 448.0;

  // Backend'in UpdateProfileRequest'teki [Range] sınırlarıyla eşleşir.
  static const _minCycleLength = 21;
  static const _maxCycleLength = 45;
  static const _minPeriodLength = 1;
  static const _maxPeriodLength = 14;

  static const _defaultCycleLength = 28;
  static const _defaultPeriodLength = 5;

  int _step = 1;
  DateTime? _lastPeriodStart;
  int _cycleLength = _defaultCycleLength;
  int _periodLength = _defaultPeriodLength;
  final Set<AvoidFlagOption> _avoidFlags = {};
  final Map<TasteTagOption, ContentReaction> _tastes = {};
  bool _isSubmitting = false;

  bool get _canContinue => _step != 1 || _lastPeriodStart != null;

  Future<void> _pickPeriodStart() async {
    final picked =
        await pickPeriodStartDate(context, initialDate: _lastPeriodStart);

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
      // Ana sayfaya geçişi router üstleniyor: sihirbaz bitince oturum
      // durumundaki `hasCompletedOnboarding` değişiyor ve yönlendirme
      // kullanıcıyı kendiliğinden alıyor.
      await _submit();
    } on ApiException catch (e) {
      if (mounted) context.showError(e.message);
    } finally {
      if (mounted) setState(() => _isSubmitting = false);
    }
  }

  Future<void> _submit() async {
    final api = ref.read(sereneApiProvider);

    final user = await api.updateMe(
      avgCycleLength: _cycleLength,
      avgPeriodLength: _periodLength,
      lastPeriodStart: _lastPeriodStart,
    );

    await api.saveTastePreferences(
      liked: _tagsWith(ContentReaction.liked),
      disliked: _tagsWith(ContentReaction.disliked),
      avoid: _avoidFlags,
    );

    // İlk döngü kaydı açıldı, yani `hasCompletedOnboarding` artık true.
    // Oturum durumu bunu duymazsa yönlendirme kullanıcıyı sihirbaza geri
    // gönderir; yanıt elimizdeyken ikinci bir `/me` isteği gereksiz.
    ref.read(authControllerProvider.notifier).updateUser(user);

    ref.invalidate(profileProvider);
    ref.invalidate(phaseTodayProvider);
    ref.invalidate(nutritionProvider);
    ref.invalidate(exerciseProvider);
  }

  Set<TasteTagOption> _tagsWith(ContentReaction reaction) => {
        for (final entry in _tastes.entries)
          if (entry.value == reaction) entry.key,
      };

  /// Çip üç durum arasında döner: nötr → severim → sevmem → nötr.
  /// Nötr "orta puan" değil "bilgi yok" demektir, bu yüzden kaydedilmez.
  void _cycleTaste(TasteTagOption tag) {
    setState(() {
      switch (_tastes[tag]) {
        case null:
          _tastes[tag] = ContentReaction.liked;
        case ContentReaction.liked:
          _tastes[tag] = ContentReaction.disliked;
        case ContentReaction.disliked:
          _tastes.remove(tag);
      }
    });
  }

  void _toggleAvoidFlag(AvoidFlagOption flag) {
    setState(() {
      if (!_avoidFlags.remove(flag)) _avoidFlags.add(flag);
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: Column(
          children: [
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
              child: _StepProgressBar(
                step: _step,
                totalSteps: _totalSteps,
                onBack: _step > 1 && !_isSubmitting ? _back : null,
              ),
            ),
            Expanded(
              child: Center(
                child: SingleChildScrollView(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 24,
                    vertical: 16,
                  ),
                  child: ConstrainedBox(
                    constraints: const BoxConstraints(maxWidth: _cardMaxWidth),
                    child: SoftShadowCard(
                      padding: const EdgeInsets.all(24),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          _stepContent(),
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

  Widget _stepContent() {
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
          subtitle: 'Emin değilsen varsayılan $_defaultCycleLength günle '
              'devam edebilirsin, sonra düzeltebilirsin.',
          child: _Stepper(
            value: _cycleLength,
            min: _minCycleLength,
            max: _maxCycleLength,
            onChanged: (value) => setState(() => _cycleLength = value),
          ),
        );
      case 3:
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
      case 4:
        return _StepContent(
          title: 'Dikkat etmemiz gereken bir şey var mı?',
          subtitle: 'İşaretlediklerin önerilere hiç girmez. Emin değilsen '
              'boş bırakabilirsin, sonra ekleyebilirsin.',
          child: _AvoidFlagPicker(
            selected: _avoidFlags,
            onToggle: _toggleAvoidFlag,
          ),
        );
      default:
        return _StepContent(
          title: 'Neleri seversin?',
          subtitle: 'Bir kez dokun: severim. İkinci dokunuş: sevmem. '
              'Bu yalnızca başlangıç noktası — gerisini önerilere verdiğin '
              'tepkilerden öğreneceğiz.',
          child: _TastePicker(selected: _tastes, onTap: _cycleTaste),
        );
    }
  }
}

/// Üstteki geri düğmesi ve adım çubuğu.
class _StepProgressBar extends StatelessWidget {
  const _StepProgressBar({
    required this.step,
    required this.totalSteps,
    required this.onBack,
  });

  static const _backSlotWidth = 40.0;
  static const _barHeight = 6.0;
  static const _barGap = 8.0;

  final int step;
  final int totalSteps;
  final VoidCallback? onBack;

  @override
  Widget build(BuildContext context) => Row(
        children: [
          SizedBox(
            width: _backSlotWidth,
            child: step > 1
                ? IconButton(
                    icon: const Icon(Icons.arrow_back),
                    color: AppColors.primary,
                    onPressed: onBack,
                  )
                : null,
          ),
          Expanded(
            child: Row(
              children: [
                for (var index = 0; index < totalSteps; index++)
                  Expanded(
                    child: Container(
                      margin: EdgeInsets.only(
                        right: index == totalSteps - 1 ? 0 : _barGap,
                      ),
                      height: _barHeight,
                      decoration: BoxDecoration(
                        color: index < step
                            ? AppColors.primaryContainer
                            : AppColors.surfaceContainerHighest,
                        borderRadius: BorderRadius.circular(4),
                      ),
                    ),
                  ),
              ],
            ),
          ),
          const SizedBox(width: _backSlotWidth),
        ],
      );
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
            style: context.text.headlineMedium
                ?.copyWith(color: AppColors.onSurface),
          ),
          const SizedBox(height: 8),
          Text(
            subtitle,
            style: context.text.bodyLarge
                ?.copyWith(color: AppColors.onSurfaceVariant),
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
    final date = value;

    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(12),
      child: Container(
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: AppColors.surfaceContainerLow,
          borderRadius: BorderRadius.circular(16),
        ),
        child: Row(
          children: [
            const Icon(Icons.edit_calendar_outlined, color: AppColors.primary),
            const SizedBox(width: 12),
            Expanded(
              child: Text(
                date == null
                    ? 'Tarih seç'
                    : DateFormat('d MMMM yyyy', 'tr').format(date),
                style: context.text.bodyLarge?.copyWith(
                  fontWeight: FontWeight.w500,
                  color: date == null
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

  static const _valueWidth = 96.0;

  final int value;
  final int min;
  final int max;
  final ValueChanged<int> onChanged;

  @override
  Widget build(BuildContext context) => Row(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          StepperButton(
            icon: Icons.remove,
            tooltip: 'Azalt',
            isLarge: true,
            onPressed: value > min ? () => onChanged(value - 1) : null,
          ),
          SizedBox(
            width: _valueWidth,
            child: Column(
              children: [
                Text(
                  '$value',
                  textAlign: TextAlign.center,
                  style: context.text.displayMedium
                      ?.copyWith(color: AppColors.primary),
                ),
                Text(
                  'gün',
                  style: context.text.bodySmall
                      ?.copyWith(color: AppColors.onSurfaceVariant),
                ),
              ],
            ),
          ),
          StepperButton(
            icon: Icons.add,
            tooltip: 'Artır',
            isLarge: true,
            onPressed: value < max ? () => onChanged(value + 1) : null,
          ),
        ],
      );
}

/// Kısıt anketi. Gruplar backend'deki sözlükle aynı: beslenme, sağlık,
/// ekipman.
class _AvoidFlagPicker extends StatelessWidget {
  const _AvoidFlagPicker({required this.selected, required this.onToggle});

  final Set<AvoidFlagOption> selected;
  final ValueChanged<AvoidFlagOption> onToggle;

  @override
  Widget build(BuildContext context) => Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisSize: MainAxisSize.min,
        children: [
          for (final group in AvoidFlagGroup.values) ...[
            _GroupTitle(group.title),
            _ChipWrap(
              children: [
                for (final flag in AvoidFlagOption.inGroup(group))
                  FilterChip(
                    label: Text(flag.label),
                    selected: selected.contains(flag),
                    onSelected: (_) => onToggle(flag),
                  ),
              ],
            ),
            const SizedBox(height: 20),
          ],
        ],
      );
}

/// Zevk anketi. Üç durumlu: seçilmemiş çip "nötr" değil "bilgi yok"
/// anlamına gelir ve sunucuya hiç gönderilmez.
class _TastePicker extends StatelessWidget {
  const _TastePicker({required this.selected, required this.onTap});

  final Map<TasteTagOption, ContentReaction> selected;
  final ValueChanged<TasteTagOption> onTap;

  @override
  Widget build(BuildContext context) => Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisSize: MainAxisSize.min,
        children: [
          for (final group in TasteTagGroup.values) ...[
            _GroupTitle(group.title),
            _ChipWrap(
              children: [
                for (final tag in TasteTagOption.inGroup(group))
                  _TasteChip(
                    label: tag.label,
                    reaction: selected[tag],
                    onTap: () => onTap(tag),
                  ),
              ],
            ),
            const SizedBox(height: 20),
          ],
        ],
      );
}

class _ChipWrap extends StatelessWidget {
  const _ChipWrap({required this.children});

  static const _spacing = 8.0;

  final List<Widget> children;

  @override
  Widget build(BuildContext context) => Wrap(
        spacing: _spacing,
        runSpacing: _spacing,
        children: children,
      );
}

class _TasteChip extends StatelessWidget {
  const _TasteChip({
    required this.label,
    required this.reaction,
    required this.onTap,
  });

  static const _radius = 20.0;

  final String label;
  final ContentReaction? reaction;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final (background, foreground, icon) = switch (reaction) {
      ContentReaction.liked => (
          AppColors.secondaryContainer,
          AppColors.primary,
          Icons.thumb_up,
        ),
      ContentReaction.disliked => (
          AppColors.surfaceContainerHighest,
          AppColors.outline,
          Icons.thumb_down,
        ),
      null => (AppColors.surfaceContainerLow, AppColors.onSurface, null),
    };

    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(_radius),
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
        decoration: BoxDecoration(
          color: background,
          borderRadius: BorderRadius.circular(_radius),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            if (icon != null) ...[
              Icon(icon, size: 14, color: foreground),
              const SizedBox(width: 6),
            ],
            Text(
              label,
              style: context.text.bodyMedium?.copyWith(
                fontWeight: FontWeight.w500,
                color: foreground,
                decoration: reaction == ContentReaction.disliked
                    ? TextDecoration.lineThrough
                    : null,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _GroupTitle extends StatelessWidget {
  const _GroupTitle(this.title);

  final String title;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.only(bottom: 12),
        child: Text(
          title,
          style: context.text.titleSmall
              ?.copyWith(color: AppColors.onSurfaceVariant),
        ),
      );
}
