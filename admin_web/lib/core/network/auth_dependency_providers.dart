import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../features/auth/data/datasources/auth_remote_datasource.dart';
import '../../features/auth/data/repositories/auth_repository_impl.dart';
import '../../features/auth/domain/repositories/auth_repository.dart';
import '../session/access_token_holder.dart';
import '../session/csrf_token_reader.dart';
import 'csrf_header_interceptor.dart';
import 'web_credentials_adapter.dart';

final accessTokenHolderProvider = Provider<AccessTokenHolder>((ref) {
  return AccessTokenHolder();
});

final csrfTokenReaderProvider = Provider<CsrfTokenReader>((ref) {
  return BrowserCsrfTokenReader();
});

/// Dio without AuthInterceptor — Auth login/refresh and Bearer-manual calls.
///
/// Still includes CSRF header attach + browser credentials when available.
final bareDioProvider = Provider<Dio>((ref) {
  final config = ref.watch(appCoreConfigProvider);
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
  ref.onDispose(() => dio.close());
  return dio;
});

final authRemoteDataSourceProvider = Provider<AuthRemoteDataSource>((ref) {
  return AuthRemoteDataSource(
    bareDio: ref.watch(bareDioProvider),
    accessTokenHolder: ref.watch(accessTokenHolderProvider),
  );
});

final authRepositoryProvider = Provider<AuthRepository>((ref) {
  return AuthRepositoryImpl(remote: ref.watch(authRemoteDataSourceProvider));
});
