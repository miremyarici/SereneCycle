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
    ref.invalidate(exerciseMinutesProvider);
    ref.invalidate(contentFeedbackProvider);
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

/// Hareket ekranındaki süre filtresi. null = "süre kısıtım yok".
class ExerciseMinutesController extends Notifier<int?> {
  @override
  int? build() => null;

  void select(int? minutes) => state = minutes;
}

final exerciseMinutesProvider =
    NotifierProvider<ExerciseMinutesController, int?>(
  ExerciseMinutesController.new,
);

final exerciseProvider = FutureProvider<PhaseContent>((ref) {
  final minutes = ref.watch(exerciseMinutesProvider);
  return ref.watch(sereneApiProvider).getExercise(minutes: minutes);
});

/// Geri bildirimin hangi listeden geldiği. Sunucu 👍/👎'yı işledikten sonra
/// sıralama değişir; hangi listeyi tazelemek gerektiğini yalnızca çağıran
/// bilir.
enum ContentSurface {
  nutrition,
  exercise;

  FutureProvider<PhaseContent> get provider =>
      this == ContentSurface.nutrition ? nutritionProvider : exerciseProvider;
}

/// Bu oturumda verilen geri bildirimler. Kalıcı kayıt sunucudadır; buradaki
/// kopya yalnızca düğmelerin seçili görünmesi içindir, bu yüzden oturum
/// kapanınca düşmesi sorun değil.
class ContentFeedbackState {
  const ContentFeedbackState({
    this.reactions = const {},
    this.completed = const {},
  });

  final Map<int, ContentReaction> reactions;
  final Set<int> completed;

  ContentFeedbackState copyWith({
    Map<int, ContentReaction>? reactions,
    Set<int>? completed,
  }) =>
      ContentFeedbackState(
        reactions: reactions ?? this.reactions,
        completed: completed ?? this.completed,
      );
}

class ContentFeedbackController extends Notifier<ContentFeedbackState> {
  @override
  ContentFeedbackState build() => const ContentFeedbackState();

  /// Aynı düğmeye tekrar basmak geri bildirimi geri almaz: gönderilmiş
  /// sinyali sunucudan silmek için bir uç yok, o yüzden yalnızca değiştirir.
  Future<void> react(
    int contentItemId,
    ContentReaction reaction, {
    required ContentSurface surface,
  }) async {
    final previous = state;

    state = state.copyWith(
      reactions: {...state.reactions, contentItemId: reaction},
    );

    try {
      await ref.read(sereneApiProvider).sendContentFeedback(
            contentItemId,
            liked: reaction == ContentReaction.liked,
          );
    } catch (_) {
      state = previous;
      rethrow;
    }

    // Sunucu bu öğenin etiketlerini artık farklı puanlıyor. Listeyi
    // tazelemezsek sıralama ancak ekran bir dahaki açılışta değişir ve
    // düğme "hiçbir şey yapmıyor" gibi görünür.
    ref.invalidate(surface.provider);
  }

  /// "Tamamladım" listeyi tazelemez: kullanıcı yaptığı hareketin işaretli
  /// hâlini görmeye devam etmeli. Sinyal yine kaydedilir, etkisi bir sonraki
  /// yüklemede sıralamaya yansır.
  Future<void> markCompleted(int contentItemId) async {
    final previous = state;

    state = state.copyWith(completed: {...state.completed, contentItemId});

    try {
      await ref
          .read(sereneApiProvider)
          .sendContentFeedback(contentItemId, completed: true);
    } catch (_) {
      state = previous;
      rethrow;
    }
  }
}

final contentFeedbackProvider =
    NotifierProvider<ContentFeedbackController, ContentFeedbackState>(
  ContentFeedbackController.new,
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
