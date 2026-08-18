import 'dart:convert';
import 'dart:typed_data';

import 'api_client.dart';
import 'models.dart';

/// Backend uç noktalarının tek erişim noktası.
class SereneApi {
  SereneApi(this._client);

  final ApiClient _client;

  // --- Auth ---

  Future<AuthResponse> login(String email, String password) async {
    final json = await _client.post(
      '/auth/login',
      data: {'email': email, 'password': password},
    );
    return AuthResponse.fromJson(json!);
  }

  Future<void> register({
    required String name,
    required String email,
    required String password,
  }) =>
      _client.post(
        '/auth/register',
        data: {'name': name, 'email': email, 'password': password},
      );

  Future<AuthResponse> verifyCode(String email, String code) async {
    final json = await _client.post(
      '/auth/verify-code',
      data: {'email': email, 'code': code},
    );
    return AuthResponse.fromJson(json!);
  }

  Future<void> resendCode(String email) =>
      _client.post('/auth/resend-code', data: {'email': email});

  Future<void> forgotPassword(String email) =>
      _client.post('/auth/forgot-password', data: {'email': email});

  Future<void> resetPassword({
    required String email,
    required String code,
    required String newPassword,
  }) =>
      _client.post(
        '/auth/reset-password',
        data: {'email': email, 'code': code, 'newPassword': newPassword},
      );

  // --- Profil ---

  Future<UserSummary> getMe() async {
    final json = await _client.get('/me');
    return UserSummary.fromJson(json!);
  }

  Future<UserSummary> updateMe({
    String? name,
    int? avgCycleLength,
    int? avgPeriodLength,
    DateTime? lastPeriodStart,
  }) async {
    final json = await _client.put('/me', data: {
      'name': ?name,
      'avgCycleLength': ?avgCycleLength,
      'avgPeriodLength': ?avgPeriodLength,
      if (lastPeriodStart != null)
        'lastPeriodStart': _formatDate(lastPeriodStart),
    });
    return UserSummary.fromJson(json!);
  }

  Future<Uint8List?> getAvatar() => _client.getBytes('/me/avatar');

  Future<UserSummary> updateAvatar({
    required String contentType,
    required Uint8List bytes,
  }) async {
    final json = await _client.put('/me/avatar', data: {
      'contentType': contentType,
      'data': base64Encode(bytes),
    });
    return UserSummary.fromJson(json!);
  }

  Future<UserSummary> removeAvatar() async {
    final json = await _client.delete('/me/avatar');
    return UserSummary.fromJson(json!);
  }

  /// Yeni adrese doğrulama kodu gönderir; adres kod onaylanana kadar
  /// değişmez.
  Future<void> requestEmailChange({
    required String newEmail,
    required String currentPassword,
  }) =>
      _client.post('/me/email/change', data: {
        'newEmail': newEmail,
        'currentPassword': currentPassword,
      });

  Future<UserSummary> confirmEmailChange(String code) async {
    final json =
        await _client.post('/me/email/confirm', data: {'code': code});
    return UserSummary.fromJson(json!);
  }

  Future<void> changePassword({
    required String currentPassword,
    required String newPassword,
  }) =>
      _client.post('/me/password', data: {
        'currentPassword': currentPassword,
        'newPassword': newPassword,
      });

  // --- Faz ve içerik ---

  Future<PhaseToday> getPhaseToday() async {
    final json = await _client.get('/phase/today');
    return PhaseToday.fromJson(json!);
  }

  Future<CalendarMonth> getCalendarMonth(int year, int month) async {
    final json = await _client.get(
      '/phase/calendar',
      queryParameters: {'year': year, 'month': month},
    );
    return CalendarMonth.fromJson(json!);
  }

  // --- Günlük kayıtlar ---

  Future<List<SymptomOption>> getSymptomOptions() async {
    final list = await _client.getList('/logs/symptoms');
    return list
        .map((e) => SymptomOption.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<DailyLogEntry> getDailyLog(DateTime date) async {
    final json = await _client.get('/logs/${_formatDate(date)}');
    return DailyLogEntry.fromJson(json!);
  }

  Future<DailyLogEntry> saveDailyLog({
    required DateTime date,
    required bool hasBleeding,
    required bool hasSpotting,
    FlowLevel? flow,
    BloodColorOption? bloodColor,
    Set<int> symptomIds = const {},
    String? note,
  }) async {
    final json = await _client.put('/logs/${_formatDate(date)}', data: {
      'hasBleeding': hasBleeding,
      'hasSpotting': hasSpotting,
      'flow': flow?.wireValue,
      'bloodColor': bloodColor?.wireValue,
      'symptomIds': symptomIds.toList(),
      'note': note,
    });
    return DailyLogEntry.fromJson(json!);
  }

  Future<PhaseContent> getNutrition() async {
    final json = await _client.get('/content/nutrition');
    return PhaseContent.fromJson(json!);
  }

  Future<PhaseContent> getExercise() async {
    final json = await _client.get('/content/exercise');
    return PhaseContent.fromJson(json!);
  }

  /// Backend DateOnly bekliyor: yyyy-MM-dd.
  static String _formatDate(DateTime date) =>
      '${date.year.toString().padLeft(4, '0')}-'
      '${date.month.toString().padLeft(2, '0')}-'
      '${date.day.toString().padLeft(2, '0')}';
}
