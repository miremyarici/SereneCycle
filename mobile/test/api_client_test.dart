import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:serene_cycle/core/api/api_client.dart';
import 'package:serene_cycle/core/api/token_storage.dart';

/// Erişim token'ı 15 dakikada dolduğu için 401 gündelik bir durum. Bu
/// testler istemcinin onu sessizce çözdüğünü ve eşzamanlı isteklerin
/// rotation'ı bozmadığını koruyor.
void main() {
  const expiredToken = 'access-expired';
  const freshToken = 'access-fresh';

  late _FakeAdapter adapter;
  late TokenStorage storage;

  ApiClient buildClient({void Function()? onSessionExpired}) => ApiClient(
        storage,
        dio: Dio(ApiClient.baseOptions)..httpClientAdapter = adapter,
        onSessionExpired: onSessionExpired,
      );

  setUp(() async {
    // Güvenli depo eklentisi test ortamında yok; paketin kendi bellek içi
    // sahtesi devreye giriyor.
    FlutterSecureStorage.setMockInitialValues({});

    adapter = _FakeAdapter(validAccessToken: freshToken);
    storage = TokenStorage();

    await storage.save(
      accessToken: expiredToken,
      refreshToken: 'refresh-1',
    );
  });

  test('401 alan istek token yenilenip tekrarlanır', () async {
    final client = buildClient();

    final response = await client.get('/me');

    expect(response?['name'], 'Deniz');
    expect(adapter.refreshCount, 1);

    // İlk deneme eski token'la reddedildi, ikincisi yenisiyle geçti.
    expect(adapter.authorizations, [
      'Bearer $expiredToken',
      'Bearer $freshToken',
    ]);

    // Yeni token çifti kalıcı: sonraki istekler baştan doğru token'la gider.
    expect(await storage.readAccessToken(), freshToken);
    expect(await storage.readRefreshToken(), 'refresh-2');
  });

  test('eşzamanlı 401 alan istekler tek yenilemeyi paylaşır', () async {
    final client = buildClient();

    final responses = await Future.wait([
      client.get('/me'),
      client.get('/phase/today'),
      client.get('/content/nutrition'),
      client.get('/content/exercise'),
      client.get('/logs/symptoms'),
    ]);

    expect(responses.every((r) => r != null), isTrue);

    // Asıl mesele bu: rotation yüzünden ikinci bir yenileme, ilkinin
    // verdiği token'ı iptal eder ve kullanıcı oturumdan düşerdi.
    expect(adapter.refreshCount, 1);
  });

  test('refresh token da reddedilirse oturum kapanır', () async {
    adapter.rejectRefresh = true;

    var expired = false;
    final client = buildClient(onSessionExpired: () => expired = true);

    await expectLater(client.get('/me'), throwsA(isA<ApiException>()));

    expect(expired, isTrue);
    expect(await storage.readAccessToken(), isNull);
    expect(await storage.readRefreshToken(), isNull);
  });

  test('ağ hatasında token silinmez', () async {
    // Sunucuya ulaşılamaması oturumun bittiği anlamına gelmez; kullanıcı
    // bağlantı geri geldiğinde kaldığı yerden devam edebilmeli.
    adapter.failWithNetworkError = true;

    var expired = false;
    final client = buildClient(onSessionExpired: () => expired = true);

    await expectLater(client.get('/me'), throwsA(isA<ApiException>()));

    expect(expired, isFalse);
    expect(await storage.readRefreshToken(), 'refresh-1');
  });

  test('giriş 401 verirse yenileme denenmez', () async {
    // Yanlış şifre bir oturum sorunu değil; auth uçlarında yenileme yolu
    // hiç çalışmamalı.
    final client = buildClient();

    await expectLater(
      client.post('/auth/login', data: const {'email': 'a@b.c'}),
      throwsA(isA<ApiException>()),
    );

    expect(adapter.refreshCount, 0);
  });
}

/// Sunucu taklidi: eski token'a 401, `/auth/refresh` çağrısına yeni bir
/// token çifti, yeni token'a 200 döner.
class _FakeAdapter implements HttpClientAdapter {
  _FakeAdapter({required this.validAccessToken});

  final String validAccessToken;

  /// Yenileme isteğinin de reddedildiği senaryo.
  bool rejectRefresh = false;

  /// Sunucuya hiç ulaşılamadığı senaryo.
  bool failWithNetworkError = false;

  int refreshCount = 0;

  /// Korumalı uçlara hangi sırayla hangi token'ın gittiği.
  final authorizations = <String>[];

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    if (failWithNetworkError) {
      throw DioException.connectionError(
        requestOptions: options,
        reason: 'test',
      );
    }

    if (options.path == '/auth/refresh') {
      refreshCount++;

      if (rejectRefresh) {
        return _json(401, {'error': 'Oturum geçersiz.'});
      }

      return _json(200, {
        'accessToken': validAccessToken,
        'refreshToken': 'refresh-2',
        'user': _user,
      });
    }

    if (options.path.startsWith('/auth/')) {
      return _json(401, {'error': 'E-posta veya şifre hatalı.'});
    }

    final authorization = options.headers['Authorization'] as String?;
    authorizations.add(authorization ?? '');

    return authorization == 'Bearer $validAccessToken'
        ? _json(200, _user)
        : _json(401, {'error': 'Oturum süresi doldu.'});
  }

  static const _user = {
    'id': 'a1b2c3',
    'name': 'Deniz',
    'email': 'deniz@ornek.com',
    'emailConfirmed': true,
    'avgCycleLength': 28,
    'avgPeriodLength': 5,
    'hasCompletedOnboarding': true,
    'avatarUpdatedAt': null,
  };

  static ResponseBody _json(int statusCode, Map<String, dynamic> body) =>
      ResponseBody.fromString(
        jsonEncode(body),
        statusCode,
        headers: {
          Headers.contentTypeHeader: [Headers.jsonContentType],
        },
      );

  @override
  void close({bool force = false}) {}
}
