import 'dart:typed_data';

import 'package:dio/dio.dart';

import 'api_config.dart';
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
  ApiClient(this._tokenStorage, {Dio? dio})
      : _dio = dio ??
            Dio(BaseOptions(
              baseUrl: ApiConfig.baseUrl,
              connectTimeout: const Duration(seconds: 10),
              receiveTimeout: const Duration(seconds: 10),
              // 4xx'i exception'a çevirmeyip kendimiz ele alıyoruz.
              validateStatus: (status) => status != null && status < 500,
            )) {
    _dio.interceptors.add(
      InterceptorsWrapper(onRequest: _attachToken),
    );
  }

  final Dio _dio;
  final TokenStorage _tokenStorage;

  Future<void> _attachToken(
    RequestOptions options,
    RequestInterceptorHandler handler,
  ) async {
    // Auth uçları token istemez; gereksiz okuma yapmayalım.
    if (!options.path.startsWith('/auth/')) {
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

  Future<Map<String, dynamic>?> delete(String path) async {
    final response = await _send(() => _dio.delete<dynamic>(path));
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
  Future<Response<dynamic>> _send(
    Future<Response<dynamic>> Function() request,
  ) async {
    late final Response<dynamic> response;

    try {
      response = await request();
    } on DioException catch (e) {
      throw ApiException(_networkMessage(e));
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
