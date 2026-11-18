import 'dart:async';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:security_mobile/core/network/auth_interceptor.dart';
import 'package:security_mobile/core/network/auth_interceptor_keys.dart';
import 'package:security_mobile/core/session/access_token_holder.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

class _Adapter implements HttpClientAdapter {
  _Adapter(this._handler);

  final Future<ResponseBody> Function(RequestOptions options) _handler;

  @override
  void close({bool force = false}) {}

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) {
    return _handler(options);
  }
}

void main() {
  late AccessTokenHolder access;
  late Dio dio;
  late int refreshCalls;
  late int expiredCalls;
  late bool logoutInProgress;
  late Completer<bool>? refreshGate;

  setUp(() {
    access = AccessTokenHolder()..setToken('access-old');
    refreshCalls = 0;
    expiredCalls = 0;
    logoutInProgress = false;
    refreshGate = null;

    dio = Dio(BaseOptions(baseUrl: 'http://localhost'));
    dio.interceptors.add(ErrorInterceptor());
    dio.interceptors.add(
      AuthInterceptor(
        dio: dio,
        accessTokenHolder: access,
        refreshSession: () async {
          refreshCalls += 1;
          if (refreshGate != null) {
            final ok = await refreshGate!.future;
            if (ok) access.setToken('access-new');
            return ok;
          }
          access.setToken('access-new');
          return true;
        },
        onSessionExpired: () => expiredCalls += 1,
        isLogoutInProgress: () => logoutInProgress,
      ),
    );
  });

  test('single-flight refresh for concurrent 401s', () async {
    var protectedHits = 0;
    refreshGate = Completer<bool>();

    dio.httpClientAdapter = _Adapter((options) async {
      if (options.path.contains('protected')) {
        protectedHits += 1;
        final auth = options.headers['Authorization'];
        final authValue = auth is List ? auth.first : auth;
        if (authValue == 'Bearer access-new') {
          return ResponseBody.fromString(
            '{"success":true,"data":{"ok":true}}',
            200,
            headers: {
              Headers.contentTypeHeader: [Headers.jsonContentType],
            },
          );
        }
        return ResponseBody.fromString(
          '{"success":false,"error":{"code":"AUTH_UNAUTHENTICATED","message":"x"}}',
          401,
          headers: {
            Headers.contentTypeHeader: [Headers.jsonContentType],
          },
        );
      }
      throw StateError('unexpected ${options.uri}');
    });

    unawaited(
      Future<void>.delayed(const Duration(milliseconds: 40), () {
        refreshGate!.complete(true);
      }),
    );

    final results = await Future.wait<Response<dynamic>>([
      dio.get<dynamic>('/protected'),
      dio.get<dynamic>('/protected'),
      dio.get<dynamic>('/protected'),
    ]);

    expect(results.every((r) => r.statusCode == 200), isTrue);
    expect(refreshCalls, 1);
    expect(expiredCalls, 0);
    expect(protectedHits, greaterThanOrEqualTo(4));
  });

  test('403 does not refresh or expire session', () async {
    dio.httpClientAdapter = _Adapter((options) async {
      return ResponseBody.fromString(
        '{"success":false,"error":{"code":"AUTH_FORBIDDEN","message":"x"}}',
        403,
        headers: {
          Headers.contentTypeHeader: [Headers.jsonContentType],
        },
      );
    });

    await expectLater(
      dio.get<dynamic>('/forbidden'),
      throwsA(isA<DioException>()),
    );
    expect(refreshCalls, 0);
    expect(expiredCalls, 0);
  });

  test('logout in progress skips refresh', () async {
    logoutInProgress = true;
    dio.httpClientAdapter = _Adapter((options) async {
      return ResponseBody.fromString(
        '{"success":false,"error":{"code":"AUTH_UNAUTHENTICATED","message":"x"}}',
        401,
        headers: {
          Headers.contentTypeHeader: [Headers.jsonContentType],
        },
      );
    });

    await expectLater(
      dio.get<dynamic>('/protected'),
      throwsA(isA<DioException>()),
    );
    expect(refreshCalls, 0);
    expect(expiredCalls, 0);
  });

  test('skipAuthRefresh prevents refresh loop', () async {
    dio.httpClientAdapter = _Adapter((options) async {
      return ResponseBody.fromString(
        '{"success":false,"error":{"code":"AUTH_UNAUTHENTICATED","message":"x"}}',
        401,
        headers: {
          Headers.contentTypeHeader: [Headers.jsonContentType],
        },
      );
    });

    await expectLater(
      dio.get<dynamic>(
        '/refresh',
        options: Options(
          extra: const {AuthInterceptorKeys.skipAuthRefresh: true},
        ),
      ),
      throwsA(isA<DioException>()),
    );
    expect(refreshCalls, 0);
  });
}
