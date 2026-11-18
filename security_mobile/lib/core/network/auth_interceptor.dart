import 'dart:async';

import 'package:dio/dio.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../session/access_token_holder.dart';
import 'auth_interceptor_keys.dart';

typedef SessionRefreshCallback = Future<bool> Function();
typedef SessionExpiredCallback = void Function();

/// Attaches Bearer access token and performs single-flight body refresh on 401.
///
/// Does not navigate. On hard refresh failure calls [onSessionExpired].
/// 403 is never treated as logout.
/// Uses a plain [Interceptor] (not [QueuedInterceptor]) so the post-refresh
/// `dio.fetch` retry cannot deadlock behind the in-flight `onError` lock.
class AuthInterceptor extends Interceptor {
  AuthInterceptor({
    required Dio dio,
    required AccessTokenHolder accessTokenHolder,
    required SessionRefreshCallback refreshSession,
    required SessionExpiredCallback onSessionExpired,
    required bool Function() isLogoutInProgress,
  })  : _dio = dio,
        _accessTokenHolder = accessTokenHolder,
        _refreshSession = refreshSession,
        _onSessionExpired = onSessionExpired,
        _isLogoutInProgress = isLogoutInProgress;

  final Dio _dio;
  final AccessTokenHolder _accessTokenHolder;
  final SessionRefreshCallback _refreshSession;
  final SessionExpiredCallback _onSessionExpired;
  final bool Function() _isLogoutInProgress;

  Future<bool>? _refreshInFlight;

  @override
  void onRequest(RequestOptions options, RequestInterceptorHandler handler) {
    final skip = options.extra[AuthInterceptorKeys.skipAuthRefresh] == true;
    final token = _accessTokenHolder.accessToken;
    if (!skip && token != null && token.isNotEmpty) {
      options.headers['Authorization'] = 'Bearer $token';
    }
    handler.next(options);
  }

  @override
  Future<void> onError(
    DioException err,
    ErrorInterceptorHandler handler,
  ) async {
    final status = err.response?.statusCode;
    final options = err.requestOptions;
    final skip = options.extra[AuthInterceptorKeys.skipAuthRefresh] == true;
    final alreadyRetried =
        options.extra[AuthInterceptorKeys.retriedAfterRefresh] == true;

    // 403 = forbidden — never refresh / never logout.
    if (status == 403) {
      handler.next(err);
      return;
    }

    if (status != 401 || skip || alreadyRetried || _isLogoutInProgress()) {
      handler.next(err);
      return;
    }

    final errorCode = _errorCode(err);
    // Only AUTH_UNAUTHENTICATED (or missing code on 401) triggers refresh.
    if (errorCode != null &&
        errorCode != ApiErrorCodes.authUnauthenticated) {
      handler.next(err);
      return;
    }

    // If a peer request already rotated the access token, retry without
    // another refresh (covers both overlapping and serialized 401 handlers).
    final failedToken = _bearerToken(options.headers['Authorization']);
    final currentToken = _accessTokenHolder.accessToken;
    final alreadyRotated = currentToken != null &&
        currentToken.isNotEmpty &&
        failedToken != null &&
        failedToken != currentToken;

    if (!alreadyRotated) {
      final refreshed = await _singleFlightRefresh();
      if (!refreshed || _isLogoutInProgress()) {
        _onSessionExpired();
        handler.next(err);
        return;
      }
    }

    final newToken = _accessTokenHolder.accessToken;
    if (newToken == null || newToken.isEmpty || _isLogoutInProgress()) {
      _onSessionExpired();
      handler.next(err);
      return;
    }

    try {
      final response = await _dio.fetch<dynamic>(
        options.copyWith(
          headers: Map<String, dynamic>.from(options.headers)
            ..['Authorization'] = 'Bearer $newToken',
          extra: Map<String, dynamic>.from(options.extra)
            ..[AuthInterceptorKeys.retriedAfterRefresh] = true,
        ),
      );
      handler.resolve(response);
    } on DioException catch (retryError) {
      handler.next(retryError);
    } catch (e, st) {
      handler.next(
        DioException(
          requestOptions: options,
          error: e,
          stackTrace: st,
        ),
      );
    }
  }

  Future<bool> _singleFlightRefresh() {
    final existing = _refreshInFlight;
    if (existing != null) return existing;

    // Assign the shared future BEFORE invoking refresh so concurrent 401
    // handlers join the same flight (no gap before the first await).
    final gate = Completer<bool>();
    _refreshInFlight = gate.future;

    () async {
      try {
        final ok = await _refreshSession();
        if (!gate.isCompleted) gate.complete(ok);
      } catch (e, st) {
        if (!gate.isCompleted) gate.completeError(e, st);
      } finally {
        if (identical(_refreshInFlight, gate.future)) {
          _refreshInFlight = null;
        }
      }
    }();

    return gate.future;
  }

  String? _errorCode(DioException err) {
    final mapped = err.error;
    if (mapped is HttpException) return mapped.errorCode;
    final data = err.response?.data;
    if (data is Map) {
      final error = data['error'];
      if (error is Map && error['code'] is String) {
        return error['code'] as String;
      }
    }
    return null;
  }

  /// Dio may store Authorization as [String] or [List<String>].
  String? _bearerToken(Object? raw) {
    String? value;
    if (raw is String) {
      value = raw;
    } else if (raw is List && raw.isNotEmpty) {
      value = raw.first?.toString();
    } else if (raw != null) {
      value = raw.toString();
    }
    if (value == null) return null;
    const prefix = 'Bearer ';
    if (value.startsWith(prefix)) {
      return value.substring(prefix.length).trim();
    }
    return value.trim();
  }
}
