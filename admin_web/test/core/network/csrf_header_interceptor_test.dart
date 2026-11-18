import 'dart:typed_data';

import 'package:admin_web/core/network/auth_interceptor_keys.dart';
import 'package:admin_web/core/network/csrf_header_interceptor.dart';
import 'package:admin_web/core/network/web_auth_constants.dart';
import 'package:admin_web/core/session/csrf_token_reader.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';

class _Adapter implements HttpClientAdapter {
  String? capturedCsrf;

  @override
  void close({bool force = false}) {}

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    capturedCsrf = options.headers[WebAuthConstants.csrfHeaderName]?.toString();
    return ResponseBody.fromString(
      '{"ok":true}',
      200,
      headers: {
        Headers.contentTypeHeader: [Headers.jsonContentType],
      },
    );
  }
}

void main() {
  test('attaches CSRF header when extra flag is set', () async {
    final adapter = _Adapter();
    final dio = Dio(BaseOptions(baseUrl: 'http://localhost'));
    dio.httpClientAdapter = adapter;
    dio.interceptors.add(
      CsrfHeaderInterceptor(
        csrfTokenReader: MemoryCsrfTokenReader('signed-csrf'),
      ),
    );

    await dio.post<dynamic>(
      '/api/auth/refresh',
      options: Options(extra: const {AuthInterceptorKeys.attachCsrf: true}),
    );

    expect(adapter.capturedCsrf, 'signed-csrf');
  });

  test('does not attach CSRF without flag', () async {
    final adapter = _Adapter();
    final dio = Dio(BaseOptions(baseUrl: 'http://localhost'));
    dio.httpClientAdapter = adapter;
    dio.interceptors.add(
      CsrfHeaderInterceptor(
        csrfTokenReader: MemoryCsrfTokenReader('signed-csrf'),
      ),
    );

    await dio.get<dynamic>('/api/auth/me');
    expect(adapter.capturedCsrf, isNull);
  });
}
