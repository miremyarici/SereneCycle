import 'dart:typed_data';

import 'package:dio/dio.dart';

import 'api_config.dart';
import 'models.dart';
import 'token_storage.dart';

/// Sunucudan gelen hataları kullanıcıya gösterilebilir mesaja çeviren tip.
class ApiException implements Exception {
  const ApiException(this.message, {this.statusCode});

  final String message;
  final int? statusCode;

  @override
  String toString() => message;
}

class ApiClient {
  ApiClient(
    this._tokenStorage, {
    Dio? dio,
    void Function()? onSessionExpired,
  })  : _onSessionExpired = onSessionExpired,
        _dio = dio ?? Dio(baseOptions) {
    _dio.interceptors.add(
      InterceptorsWrapper(onRequest: _attachToken),
    );
  }

  /// Dışarıdan Dio verilirken de aynı ayarlar geçerli olmalı, bu yüzden tek
  /// yerde. Özellikle [BaseOptions.validateStatus]: 4xx exception'a
  /// çevrilirse [_send] 401'i hiç göremez ve token yenileme devreye girmez.
  static BaseOptions get baseOptions => BaseOptions(
        baseUrl: ApiConfig.baseUrl,
        connectTimeout: const Duration(seconds: 10),
        receiveTimeout: const Duration(seconds: 10),
        validateStatus: (status) => status != null && status < 500,
      );

  final Dio _dio;
  final TokenStorage _tokenStorage;

  /// Refresh token da reddedildiğinde çağrılır: oturum gerçekten bitmiştir
  /// ve uygulamanın kullanıcıyı giriş ekranına alması gerekir.
  final void Function()? _onSessionExpired;

  /// Süren yenileme. Aynı anda 401 alan bütün istekler bunu bekler; yoksa
  /// beş provider aynı anda yenileme tetikler ve rotation yüzünden yalnızca
  /// biri geçerli token alır, kalanı oturumdan düşerdi.
  Future<bool>? _refreshInFlight;

  Future<void> _attachToken(
    RequestOptions options,
    RequestInterceptorHandler handler,
  ) async {
    // Auth uçları token istemez; gereksiz okuma yapmayalım.
    if (!_isAuthEndpoint(options.path)) {
      final token = await _tokenStorage.readAccessToken();
      if (token != null) {
        options.headers['Authorization'] = 'Bearer $token';
      }
    }

    handler.next(options);
  }

  Future<Map<String, dynamic>?> get(
    String path, {
    Map<String, dynamic>? queryParameters,
  }) async {
    final response = await _send(
      () => _dio.get<dynamic>(path, queryParameters: queryParameters),
    );
    return _asJson(response.data);
  }

  Future<Map<String, dynamic>?> post(
    String path, {
    Object? data,
  }) async {
    final response = await _send(() => _dio.post<dynamic>(path, data: data));
    return _asJson(response.data);
  }

  /// Liste dönen uçlar için: gövde JSON dizisi olduğunda [get] `null` döner.
  Future<List<dynamic>> getList(String path) async {
    final response = await _send(() => _dio.get<dynamic>(path));
    return response.data is List ? response.data as List<dynamic> : const [];
  }

  Future<Map<String, dynamic>?> put(String path, {Object? data}) async {
    final response = await _send(() => _dio.put<dynamic>(path, data: data));
    return _asJson(response.data);
  }

  /// [data]: hesap silme gibi gövde taşıyan silme istekleri için.
  Future<Map<String, dynamic>?> delete(String path, {Object? data}) async {
    final response = await _send(() => _dio.delete<dynamic>(path, data: data));
    return _asJson(response.data);
  }

  /// Profil fotoğrafı gibi ikili içerikler için. Token gerektiği için
  /// `Image.network` yerine bu yol kullanılır.
  Future<Uint8List?> getBytes(String path) async {
    final response = await _send(
      () => _dio.get<List<int>>(
        path,
        options: Options(responseType: ResponseType.bytes),
      ),
    );

    final data = response.data as List<int>?;
    return data == null ? null : Uint8List.fromList(data);
  }

  /// 204 gibi gövdesiz yanıtlarda web'deki Dio, `null` yerine boş string
  /// döndürüyor — bunu doğrudan `Map`'e cast etmek çöküyordu.
  static Map<String, dynamic>? _asJson(dynamic data) =>
      data is Map<String, dynamic> ? data : null;

  /// Ağ hatalarını ve 4xx yanıtlarını tek bir [ApiException]'a indirger.
  ///
  /// Erişim token'ı 15 dakikada dolduğu için 401 istisnai değil beklenen bir
  /// durum: token sessizce yenilenir ve istek bir kez tekrarlanır. [request]
  /// bir kapanış olduğu için tekrar çağrılması yeni bir istek kurar ve
  /// interceptor tazelenmiş token'ı ekler.
  Future<Response<dynamic>> _send(
    Future<Response<dynamic>> Function() request,
  ) async {
    var response = await _run(request);

    if (response.statusCode == 401 &&
        !_isAuthEndpoint(response.requestOptions.path)) {
      final used = response.requestOptions.headers['Authorization'] as String?;

      if (await _refreshSession(used)) {
        response = await _run(request);
      }
    }

    final status = response.statusCode ?? 0;

    if (status >= 400) {
      throw ApiException(
        _serverMessage(response.data) ?? 'Bir şeyler ters gitti.',
        statusCode: status,
      );
    }

    return response;
  }

  Future<Response<dynamic>> _run(
    Future<Response<dynamic>> Function() request,
  ) async {
    try {
      return await request();
    } on DioException catch (e) {
      throw ApiException(_networkMessage(e));
    }
  }

  /// Süren bir yenileme varsa ona katılır, yoksa başlatır.
  Future<bool> _refreshSession(String? usedAuthorization) {
    return _refreshInFlight ??= _refresh(usedAuthorization).whenComplete(() {
      _refreshInFlight = null;
    });
  }

  Future<bool> _refresh(String? usedAuthorization) async {
    // Bu istek yoldayken başka bir istek zaten yenilemiş olabilir. O hâlde
    // tekrar yenilemek, henüz kullanılmamış geçerli bir token'ı boşuna
    // döndürür — isteği yeni token'la tekrarlamak yeter.
    final current = await _tokenStorage.readAccessToken();

    if (current != null && 'Bearer $current' != usedAuthorization) {
      return true;
    }

    final refreshToken = await _tokenStorage.readRefreshToken();

    if (refreshToken == null) return false;

    final Response<dynamic> response;

    try {
      response = await _dio.post<dynamic>(
        '/auth/refresh',
        data: {'refreshToken': refreshToken},
      );
    } on DioException {
      // Ağ hatası oturumun bittiği anlamına gelmez; token'lara dokunmuyoruz
      // ki kullanıcı bağlantı geri geldiğinde kaldığı yerden devam etsin.
      return false;
    }

    final body = _asJson(response.data);

    if ((response.statusCode ?? 0) < 400 && body != null) {
      final tokens = AuthResponse.fromJson(body);

      await _tokenStorage.save(
        accessToken: tokens.accessToken,
        refreshToken: tokens.refreshToken,
      );

      return true;
    }

    // Sunucu refresh token'ı da reddetti: oturum gerçekten bitmiş.
    await _tokenStorage.clear();
    _onSessionExpired?.call();

    return false;
  }

  static bool _isAuthEndpoint(String path) => path.startsWith('/auth/');

  static String? _serverMessage(dynamic data) {
    if (data is Map<String, dynamic>) {
      final error = data['error'];
      if (error is String && error.isNotEmpty) return error;

      // ASP.NET model doğrulama hataları ValidationProblemDetails formatında.
      final errors = data['errors'];
      if (errors is Map<String, dynamic> && errors.isNotEmpty) {
        final first = errors.values.first;
        if (first is List && first.isNotEmpty) return first.first.toString();
      }

      final title = data['title'];
      if (title is String && title.isNotEmpty) return title;
    }

    return null;
  }

  static String _networkMessage(DioException e) => switch (e.type) {
        DioExceptionType.connectionTimeout ||
        DioExceptionType.receiveTimeout ||
        DioExceptionType.sendTimeout =>
          'Sunucu yanıt vermedi. Bağlantını kontrol et.',
        DioExceptionType.connectionError =>
          'Sunucuya ulaşılamadı. API çalışıyor mu?',
        _ => 'Beklenmeyen bir bağlantı hatası oluştu.',
      };
}
