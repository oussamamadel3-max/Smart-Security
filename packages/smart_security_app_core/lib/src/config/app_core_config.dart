import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../constants/network_constants.dart';

/// Optional navigation gate: may this [roleCode] open [path]?
typedef RoleAccessCallback = bool Function(String? roleCode, String path);

/// Host-app configuration. Override [appCoreConfigProvider] in `ProviderScope`.
///
/// Auth refresh / session storage are **not** configured here — each app
/// composes them in T-02 (Mobile body vs Web Cookie/CSRF).
class AppCoreConfig {
  const AppCoreConfig({
    required this.baseUrl,
    this.connectTimeout = NetworkConstants.connectTimeout,
    this.receiveTimeout = NetworkConstants.receiveTimeout,
    this.sendTimeout = NetworkConstants.sendTimeout,
    this.defaultHeaders = NetworkConstants.defaultHeaders,
    this.httpClientAdapter,
    this.roleAllows,
  });

  final String baseUrl;
  final Duration connectTimeout;
  final Duration receiveTimeout;
  final Duration sendTimeout;
  final Map<String, String> defaultHeaders;
  final HttpClientAdapter? httpClientAdapter;
  final RoleAccessCallback? roleAllows;
}

/// Must be overridden in the host app.
final appCoreConfigProvider = Provider<AppCoreConfig>((ref) {
  throw UnimplementedError(
    'Override appCoreConfigProvider in ProviderScope with AppCoreConfig(baseUrl: ...).',
  );
});
