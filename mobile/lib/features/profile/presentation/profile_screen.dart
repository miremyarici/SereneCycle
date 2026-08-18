import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/api/models.dart';
import '../../../core/providers/app_providers.dart';
import '../../../core/router/route_paths.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/widgets/async_view.dart';
import '../../../core/widgets/soft_shadow_card.dart';

class ProfileScreen extends ConsumerWidget {
  const ProfileScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final profile = ref.watch(profileProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text(
          'Profil',
          style: TextStyle(
            fontSize: 20,
            fontWeight: FontWeight.w600,
            color: AppColors.primary,
          ),
        ),
        centerTitle: true,
      ),
      body: AsyncView(
        value: profile,
        onRetry: () => ref.invalidate(profileProvider),
        builder: (user) => ListView(
          padding: const EdgeInsets.fromLTRB(24, 16, 24, 24),
          children: [
            _Header(user: user),
            const SizedBox(height: 24),
            const _EditProfileCard(),
            const SizedBox(height: 20),
            _SettingsCard(user: user),
            const SizedBox(height: 20),
            const _PreferencesCard(),
            const SizedBox(height: 24),
            _LogOutButton(
              onPressed: () async {
                await ref.read(authControllerProvider.notifier).signOut();
                if (context.mounted) context.go(RoutePaths.login);
              },
            ),
          ],
        ),
      ),
    );
  }
}

class _Header extends ConsumerWidget {
  const _Header({required this.user});

  final UserSummary user;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    // Fotoğraf henüz inmediyse baş harfler görünür; ayrı bir yükleme
    // göstergesi profil başlığında gereksiz gürültü olurdu.
    final avatar = ref.watch(avatarProvider).value;

    return Column(
      children: [
        Container(
          width: 96,
          height: 96,
          alignment: Alignment.center,
          clipBehavior: Clip.antiAlias,
          decoration: const BoxDecoration(
            color: AppColors.primaryContainer,
            shape: BoxShape.circle,
          ),
          child: avatar == null
              ? Text(
                  user.initials,
                  style: const TextStyle(
                    fontSize: 36,
                    fontWeight: FontWeight.w600,
                    color: AppColors.onPrimaryContainer,
                  ),
                )
              : Image.memory(
                  avatar,
                  width: 96,
                  height: 96,
                  fit: BoxFit.cover,
                ),
        ),
        const SizedBox(height: 16),
        Text(
          user.name,
          style: const TextStyle(
            fontSize: 24,
            fontWeight: FontWeight.w600,
            color: AppColors.primary,
          ),
        ),
        const SizedBox(height: 4),
        Text(
          user.email,
          style: const TextStyle(
            fontSize: 16,
            color: AppColors.onSurfaceVariant,
          ),
        ),
      ],
    );
  }
}

/// Döngü ayarlarının hemen üstünde: fotoğraf, isim, e-posta ve parola.
class _EditProfileCard extends StatelessWidget {
  const _EditProfileCard();

  @override
  Widget build(BuildContext context) => SoftShadowCard(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const _CardTitle('PROFİL BİLGİLERİNİ DÜZENLE'),
            const SizedBox(height: 8),
            _ActionRow(
              icon: Icons.manage_accounts_outlined,
              label: 'Fotoğraf, isim, e-posta ve parola',
              onTap: () => context.push(RoutePaths.accountSettings),
            ),
          ],
        ),
      );
}

class _SettingsCard extends StatelessWidget {
  const _SettingsCard({required this.user});

  final UserSummary user;

  @override
  Widget build(BuildContext context) => SoftShadowCard(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const _CardTitle('DÖNGÜ AYARLARI'),
            const SizedBox(height: 8),
            _SettingRow(
              icon: Icons.calendar_month_outlined,
              label: 'Ortalama döngü uzunluğu',
              value: '${user.avgCycleLength} gün',
            ),
            const Divider(height: 24, color: AppColors.outlineVariant),
            _SettingRow(
              icon: Icons.water_drop_outlined,
              label: 'Adet süresi',
              value: '${user.avgPeriodLength} gün',
            ),
            const Divider(height: 24, color: AppColors.outlineVariant),
            _ActionRow(
              icon: Icons.edit_calendar_outlined,
              label: 'Adet döngünü düzenle',
              onTap: () => context.push(RoutePaths.cycleSettings),
            ),
          ],
        ),
      );
}

/// Bir alt sayfaya götüren satır — sağdaki ok yönlendirme olduğunu belli eder.
class _ActionRow extends StatelessWidget {
  const _ActionRow({
    required this.icon,
    required this.label,
    required this.onTap,
  });

  final IconData icon;
  final String label;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.symmetric(vertical: 4),
          child: Row(
            children: [
              Container(
                width: 40,
                height: 40,
                decoration: const BoxDecoration(
                  color: AppColors.surfaceContainer,
                  shape: BoxShape.circle,
                ),
                child: Icon(icon, size: 20, color: AppColors.secondary),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Text(
                  label,
                  style: const TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.w500,
                    color: AppColors.primary,
                  ),
                ),
              ),
              const Icon(Icons.chevron_right, color: AppColors.outline),
            ],
          ),
        ),
      );
}

class _PreferencesCard extends StatelessWidget {
  const _PreferencesCard();

  @override
  Widget build(BuildContext context) => const SoftShadowCard(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            _CardTitle('TERCİHLER'),
            SizedBox(height: 8),
            _SettingRow(
              icon: Icons.notifications_outlined,
              label: 'Bildirimler',
              value: 'Yakında',
            ),
            Divider(height: 24, color: AppColors.outlineVariant),
            _SettingRow(
              icon: Icons.lock_outline,
              label: 'Gizlilik ve güvenlik',
              value: 'Yakında',
            ),
          ],
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

class _SettingRow extends StatelessWidget {
  const _SettingRow({
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
          Container(
            width: 40,
            height: 40,
            decoration: const BoxDecoration(
              color: AppColors.surfaceContainer,
              shape: BoxShape.circle,
            ),
            child: Icon(icon, size: 20, color: AppColors.secondary),
          ),
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
                  value,
                  style: const TextStyle(
                    fontSize: 12,
                    color: AppColors.onSurfaceVariant,
                  ),
                ),
              ],
            ),
          ),
        ],
      );
}

class _LogOutButton extends StatelessWidget {
  const _LogOutButton({required this.onPressed});

  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) => InkWell(
        onTap: onPressed,
        borderRadius: BorderRadius.circular(16),
        child: SoftShadowCard(
          child: Row(
            children: [
              Container(
                width: 40,
                height: 40,
                decoration: BoxDecoration(
                  color: AppColors.errorContainer.withValues(alpha: 0.5),
                  shape: BoxShape.circle,
                ),
                child: const Icon(
                  Icons.logout,
                  size: 20,
                  color: AppColors.error,
                ),
              ),
              const SizedBox(width: 16),
              const Text(
                'Çıkış Yap',
                style: TextStyle(
                  fontSize: 20,
                  fontWeight: FontWeight.w600,
                  color: AppColors.error,
                ),
              ),
            ],
          ),
        ),
      );
}
