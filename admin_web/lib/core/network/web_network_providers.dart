import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../session/auth_session_controller.dart';
import 'auth_dependency_providers.dart';
import 'auth_interceptor.dart';
import 'csrf_header_interceptor.dart';
import 'web_credentials_adapter.dart';

export 'auth_dependency_providers.dart';

/// App-wide Dio with Bearer + single-flight cookie refresh for feature APIs.
final webDioProvider = Provider<Dio>((ref) {
  final config = ref.watch(appCoreConfigProvider);
  final access = ref.watch(accessTokenHolderProvider);
  final session = ref.read(authSessionProvider.notifier);
  final adapter = config.httpClientAdapter ?? createWebCredentialsAdapter();

  final dio = DioClientSetup.build(
    config: AppCoreConfig(
      baseUrl: config.baseUrl,
      connectTimeout: config.connectTimeout,
      receiveTimeout: config.receiveTimeout,
      sendTimeout: config.sendTimeout,
      defaultHeaders: config.defaultHeaders,
      httpClientAdapter: adapter,
      roleAllows: config.roleAllows,
    ),
  );
  dio.interceptors.insert(
    0,
    CsrfHeaderInterceptor(csrfTokenReader: ref.watch(csrfTokenReaderProvider)),
  );
  dio.interceptors.add(
    AuthInterceptor(
      dio: dio,
      accessTokenHolder: access,
      refreshSession: session.refreshSession,
      onSessionExpired: session.markSessionExpired,
      isLogoutInProgress: () => session.isLogoutInProgress,
    ),
  );
  ref.onDispose(() => dio.close());
  return dio;
});
