import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../session/auth_session_controller.dart';
import 'auth_dependency_providers.dart';
import 'auth_interceptor.dart';

export 'auth_dependency_providers.dart';

/// App-wide Dio with Bearer + single-flight refresh for feature APIs.
final mobileDioProvider = Provider<Dio>((ref) {
  final config = ref.watch(appCoreConfigProvider);
  final access = ref.watch(accessTokenHolderProvider);
  final session = ref.read(authSessionProvider.notifier);

  // AuthInterceptor must be last so it runs first on the reverse error chain
  // (after ErrorInterceptor is registered by DioClientSetup).
  final dio = DioClientSetup.build(config: config);
  dio.interceptors.add(
    AuthInterceptor(
      dio: dio,
      accessTokenHolder: access,
      refreshSession: session.refreshSession,
      onSessionExpired: session.markSessionExpired,
      isLogoutInProgress: () => session.isLogoutInProgress,
    ),
  );
  return dio;
});
