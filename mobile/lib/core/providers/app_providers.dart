import 'dart:typed_data';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../api/api_client.dart';
import '../api/models.dart';
import '../api/serene_api.dart';
import '../api/token_storage.dart';

final tokenStorageProvider = Provider<TokenStorage>((ref) => TokenStorage());

final apiClientProvider = Provider<ApiClient>(
  (ref) => ApiClient(ref.watch(tokenStorageProvider)),
);

final sereneApiProvider = Provider<SereneApi>(
  (ref) => SereneApi(ref.watch(apiClientProvider)),
);

/// Giriş yapmış kullanıcı. null ise oturum yok.
class AuthController extends Notifier<UserSummary?> {
  @override
  UserSummary? build() => null;

  Future<void> signIn(String email, String password) async {
    final api = ref.read(sereneApiProvider);
    final response = await api.login(email, password);

    await ref.read(tokenStorageProvider).save(
          accessToken: response.accessToken,
          refreshToken: response.refreshToken,
        );

    state = response.user;
  }

  Future<void> completeVerification(AuthResponse response) async {
    await ref.read(tokenStorageProvider).save(
          accessToken: response.accessToken,
          refreshToken: response.refreshToken,
        );

    state = response.user;
  }

  Future<void> signOut() async {
    await ref.read(tokenStorageProvider).clear();
    state = null;
    // Kullanıcıya özel verileri de düşür ki sonraki oturum temiz başlasın.
    ref.invalidate(phaseTodayProvider);
    ref.invalidate(nutritionProvider);
    ref.invalidate(exerciseProvider);
    ref.invalidate(profileProvider);
    ref.invalidate(avatarProvider);
    ref.invalidate(calendarMonthProvider);
    ref.invalidate(dailyLogProvider);
  }
}

final authControllerProvider =
    NotifierProvider<AuthController, UserSummary?>(AuthController.new);

// --- Veri sağlayıcıları ---

final phaseTodayProvider = FutureProvider<PhaseToday>(
  (ref) => ref.watch(sereneApiProvider).getPhaseToday(),
);

final nutritionProvider = FutureProvider<PhaseContent>(
  (ref) => ref.watch(sereneApiProvider).getNutrition(),
);

final exerciseProvider = FutureProvider<PhaseContent>(
  (ref) => ref.watch(sereneApiProvider).getExercise(),
);

final profileProvider = FutureProvider<UserSummary>(
  (ref) => ref.watch(sereneApiProvider).getMe(),
);

/// Profil fotoğrafının baytları. Fotoğraf yoksa null döner; `avatarUpdatedAt`
/// değiştiğinde profil yeniden yüklendiği için bu da tazelenir.
final avatarProvider = FutureProvider<Uint8List?>((ref) async {
  final user = await ref.watch(profileProvider.future);
  if (!user.hasAvatar) return null;
  return ref.watch(sereneApiProvider).getAvatar();
});

/// Adet kaydı ekranındaki belirti çipleri. Oturum boyunca değişmez.
final symptomOptionsProvider = FutureProvider<List<SymptomOption>>(
  (ref) => ref.watch(sereneApiProvider).getSymptomOptions(),
);

/// Takvim ekranındaki ay. Anahtar: ayın ilk günü.
final calendarMonthProvider =
    FutureProvider.family<CalendarMonth, DateTime>((ref, month) {
  return ref.watch(sereneApiProvider).getCalendarMonth(month.year, month.month);
});

/// Tek bir günün kaydı. Anahtar: gün (saat bilgisi olmadan).
final dailyLogProvider =
    FutureProvider.family<DailyLogEntry, DateTime>((ref, date) {
  return ref.watch(sereneApiProvider).getDailyLog(date);
});
