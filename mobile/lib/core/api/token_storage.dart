import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// Token'ları cihazın güvenli deposunda tutar (Android'de EncryptedShared
/// Preferences). Web'de güvenli depo yok; orada yalnızca geliştirme
/// amacıyla bellekte tutulur.
class TokenStorage {
  // flutter_secure_storage 11'den itibaren Android tarafı varsayılan olarak
  // şifreli depolama kullanıyor; ayrıca yapılandırma gerekmiyor.
  TokenStorage([FlutterSecureStorage? storage])
      : _storage = storage ?? const FlutterSecureStorage();

  static const _accessTokenKey = 'access_token';
  static const _refreshTokenKey = 'refresh_token';

  final FlutterSecureStorage _storage;

  // Web'de secure storage gerçek bir güvenlik sağlamadığı için bellek içi.
  static String? _memoryAccessToken;
  static String? _memoryRefreshToken;

  Future<String?> readAccessToken() async {
    if (kIsWeb) return _memoryAccessToken;
    return _storage.read(key: _accessTokenKey);
  }

  Future<String?> readRefreshToken() async {
    if (kIsWeb) return _memoryRefreshToken;
    return _storage.read(key: _refreshTokenKey);
  }

  Future<void> save({
    required String accessToken,
    required String refreshToken,
  }) async {
    if (kIsWeb) {
      _memoryAccessToken = accessToken;
      _memoryRefreshToken = refreshToken;
      return;
    }

    await _storage.write(key: _accessTokenKey, value: accessToken);
    await _storage.write(key: _refreshTokenKey, value: refreshToken);
  }

  Future<void> clear() async {
    if (kIsWeb) {
      _memoryAccessToken = null;
      _memoryRefreshToken = null;
      return;
    }

    await _storage.delete(key: _accessTokenKey);
    await _storage.delete(key: _refreshTokenKey);
  }
}
