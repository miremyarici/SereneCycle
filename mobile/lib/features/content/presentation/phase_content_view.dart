import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/api/api_client.dart';
import '../../../core/api/models.dart';
import '../../../core/providers/app_providers.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_typography.dart';
import '../../../core/widgets/app_snack_bar.dart';
import '../../../core/widgets/section_title.dart';
import '../../../core/widgets/soft_shadow_card.dart';

/// Beslenme ve Hareket ekranları aynı iskeleti paylaşır: "öncelik ver"
/// kartı, "sınırlı tut" kartı ve altta tıbbi uyarı.
class PhaseContentView extends StatelessWidget {
  const PhaseContentView({
    required this.content,
    required this.surface,
    required this.recommendedTitle,
    required this.limitedTitle,
    required this.recommendedIcon,
    required this.limitedIcon,
    this.footer,
    this.recommendedEmptyMessage,
    this.showCompletedAction = false,
    super.key,
  });

  final PhaseContent content;

  /// Geri bildirim sonrası hangi listenin tazeleneceğini belirler.
  final ContentSurface surface;

  final String recommendedTitle;
  final String limitedTitle;
  final IconData recommendedIcon;
  final IconData limitedIcon;
  final Widget? footer;

  /// Öneri listesi boş kaldığında gösterilecek metin. Katalog geniş olduğu
  /// için bu durumun gerçek sebebi neredeyse her zaman kısıtlardır ("15
  /// dakika" + ekipman yok gibi), o yüzden ekran kendi açıklamasını verebilir.
  final String? recommendedEmptyMessage;

  /// Hareket önerilerinde "tamamladım" da bir sinyal; beslenmede değil.
  final bool showCompletedAction;

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.fromLTRB(24, 16, 24, 24),
      children: [
        SectionTitle(content.phaseName),
        const SizedBox(height: 16),
        _ContentCard(
          title: recommendedTitle,
          items: content.recommended,
          icon: recommendedIcon,
          iconBackground: AppColors.secondaryContainer,
          iconColor: AppColors.primary,
          emptyMessage: recommendedEmptyMessage,
          // Geri bildirim yalnızca önerilerde sorulur: "sınırlı tut"
          // listesi bir öneri değil, bir uyarı.
          surface: surface,
          showCompletedAction: showCompletedAction,
        ),
        const SizedBox(height: 20),
        _ContentCard(
          title: limitedTitle,
          items: content.limited,
          icon: limitedIcon,
          iconBackground: AppColors.surfaceContainerHigh,
          iconColor: AppColors.tertiary,
        ),
        if (footer != null) ...[
          const SizedBox(height: 24),
          footer!,
        ],
        const SizedBox(height: 20),
        Text(
          content.disclaimer,
          textAlign: TextAlign.center,
          style: context.text.bodySmall?.copyWith(color: AppColors.outline),
        ),
      ],
    );
  }
}

class _ContentCard extends StatelessWidget {
  const _ContentCard({
    required this.title,
    required this.items,
    required this.icon,
    required this.iconBackground,
    required this.iconColor,
    this.surface,
    this.emptyMessage,
    this.showCompletedAction = false,
  });

  static const _rowSpacing = 16.0;

  final String title;
  final List<ContentItem> items;
  final IconData icon;
  final Color iconBackground;
  final Color iconColor;

  /// Verildiğinde 👍/👎 çubuğu çizilir; yalnızca öneri listelerinde dolu.
  final ContentSurface? surface;

  final String? emptyMessage;
  final bool showCompletedAction;

  @override
  Widget build(BuildContext context) {
    return SoftShadowCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            title,
            style: context.text.headlineSmall
                ?.copyWith(color: AppColors.primary),
          ),
          const SizedBox(height: 12),
          if (items.isEmpty)
            _EmptyMessage(emptyMessage)
          else
            for (final (index, item) in items.indexed)
              Padding(
                padding: EdgeInsets.only(
                  bottom: index == items.length - 1 ? 0 : _rowSpacing,
                ),
                child: _ContentRow(
                  item: item,
                  icon: icon,
                  iconBackground: iconBackground,
                  iconColor: iconColor,
                  surface: surface,
                  showCompletedAction: showCompletedAction,
                ),
              ),
        ],
      ),
    );
  }
}

class _EmptyMessage extends StatelessWidget {
  const _EmptyMessage(this.message);

  final String? message;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.symmetric(vertical: 8),
        child: Text(
          message ?? 'Bu faz için henüz içerik eklenmedi.',
          style: context.text.bodyMedium
              ?.copyWith(color: AppColors.onSurfaceVariant),
        ),
      );
}

/// Tek bir öneri satırı: yuvarlak ikon, başlık, gövde ve —önerilerde—
/// geri bildirim çubuğu.
class _ContentRow extends StatelessWidget {
  const _ContentRow({
    required this.item,
    required this.icon,
    required this.iconBackground,
    required this.iconColor,
    required this.surface,
    required this.showCompletedAction,
  });

  static const _iconDiameter = 40.0;

  final ContentItem item;
  final IconData icon;
  final Color iconBackground;
  final Color iconColor;
  final ContentSurface? surface;
  final bool showCompletedAction;

  @override
  Widget build(BuildContext context) {
    final feedbackSurface = surface;

    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Container(
          width: _iconDiameter,
          height: _iconDiameter,
          decoration: BoxDecoration(
            color: iconBackground,
            shape: BoxShape.circle,
          ),
          child: Icon(icon, size: 20, color: iconColor),
        ),
        const SizedBox(width: 16),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              _ItemHeadline(item: item),
              const SizedBox(height: 2),
              Text(
                item.body,
                style: context.text.bodyMedium
                    ?.copyWith(color: AppColors.onSurfaceVariant),
              ),
              if (feedbackSurface != null) ...[
                const SizedBox(height: 4),
                _ContentFeedbackBar(
                  item: item,
                  surface: feedbackSurface,
                  showCompletedAction: showCompletedAction,
                ),
              ],
            ],
          ),
        ),
      ],
    );
  }
}

/// Başlık ve —varsa— süre rozeti.
class _ItemHeadline extends StatelessWidget {
  const _ItemHeadline({required this.item});

  final ContentItem item;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Flexible(
          child: Text(
            item.title,
            style:
                context.text.titleSmall?.copyWith(color: AppColors.onSurface),
          ),
        ),
        if (item.durationMinutes != null) ...[
          const SizedBox(width: 8),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
            decoration: BoxDecoration(
              color: AppColors.surfaceContainerHigh,
              borderRadius: BorderRadius.circular(8),
            ),
            child: Text(
              '${item.durationMinutes} dk',
              style: context.text.labelSmall
                  ?.copyWith(color: AppColors.onSurfaceVariant),
            ),
          ),
        ],
      ],
    );
  }
}

/// 👍 / 👎 (ve hareketlerde "tamamladım"). Tek bir dokunuş, öneri motorunun
/// öğrendiği tek sinyaldir.
class _ContentFeedbackBar extends ConsumerWidget {
  const _ContentFeedbackBar({
    required this.item,
    required this.surface,
    this.showCompletedAction = false,
  });

  final ContentItem item;
  final ContentSurface surface;
  final bool showCompletedAction;

  Future<void> _send(
    BuildContext context,
    Future<void> Function() action,
  ) async {
    try {
      await action();
    } on ApiException catch (e) {
      if (context.mounted) context.showError(e.message);
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    // `select` olmadan tek bir 👍 bütün listedeki çubukları yeniden
    // çizdiriyordu; burada yalnızca bu öğenin durumu dinleniyor.
    final reaction = ref.watch(
      contentFeedbackProvider.select((state) => state.reactions[item.id]),
    );
    final isCompleted = ref.watch(
      contentFeedbackProvider.select(
        (state) => state.completed.contains(item.id),
      ),
    );
    final controller = ref.read(contentFeedbackProvider.notifier);

    return Row(
      children: [
        _FeedbackButton(
          icon: Icons.thumb_up_outlined,
          selectedIcon: Icons.thumb_up,
          tooltip: 'Beğendim',
          isSelected: reaction == ContentReaction.liked,
          onPressed: () => _send(
            context,
            () => controller.react(
              item.id,
              ContentReaction.liked,
              surface: surface,
            ),
          ),
        ),
        _FeedbackButton(
          icon: Icons.thumb_down_outlined,
          selectedIcon: Icons.thumb_down,
          tooltip: 'Bana göre değil',
          isSelected: reaction == ContentReaction.disliked,
          onPressed: () => _send(
            context,
            () => controller.react(
              item.id,
              ContentReaction.disliked,
              surface: surface,
            ),
          ),
        ),
        if (showCompletedAction)
          _CompletedButton(
            isCompleted: isCompleted,
            onPressed: () =>
                _send(context, () => controller.markCompleted(item.id)),
          ),
      ],
    );
  }
}

class _CompletedButton extends StatelessWidget {
  const _CompletedButton({required this.isCompleted, required this.onPressed});

  final bool isCompleted;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) => TextButton.icon(
        onPressed: isCompleted ? null : onPressed,
        icon: Icon(
          isCompleted ? Icons.check_circle : Icons.check_circle_outline,
          size: 18,
        ),
        label: Text(isCompleted ? 'Tamamlandı' : 'Tamamladım'),
        style: TextButton.styleFrom(
          foregroundColor: AppColors.primary,
          disabledForegroundColor: AppColors.primary,
          visualDensity: VisualDensity.compact,
        ),
      );
}

class _FeedbackButton extends StatelessWidget {
  const _FeedbackButton({
    required this.icon,
    required this.selectedIcon,
    required this.tooltip,
    required this.isSelected,
    required this.onPressed,
  });

  final IconData icon;
  final IconData selectedIcon;
  final String tooltip;
  final bool isSelected;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) => IconButton(
        onPressed: onPressed,
        tooltip: tooltip,
        iconSize: 18,
        visualDensity: VisualDensity.compact,
        icon: Icon(
          isSelected ? selectedIcon : icon,
          color: isSelected ? AppColors.primary : AppColors.outline,
        ),
      );
}
