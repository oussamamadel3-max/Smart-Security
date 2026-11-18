import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../features/auth/data/datasources/auth_remote_datasource.dart';
import '../../features/auth/data/repositories/auth_repository_impl.dart';
import '../../features/auth/domain/repositories/auth_repository.dart';
import '../session/access_token_holder.dart';
import '../session/refresh_token_store.dart';

final accessTokenHolderProvider = Provider<AccessTokenHolder>((ref) {
  return AccessTokenHolder();
});

final flutterSecureStorageProvider = Provider<FlutterSecureStorage>((ref) {
  return const FlutterSecureStorage();
});

final refreshTokenStoreProvider = Provider<RefreshTokenStore>((ref) {
  return SecureRefreshTokenStore(ref.watch(flutterSecureStorageProvider));
});

/// Dio without AuthInterceptor — Auth login/refresh and Bearer-manual calls.
final bareDioProvider = Provider<Dio>((ref) {
  final config = ref.watch(appCoreConfigProvider);
  return DioClientSetup.build(config: config);
});

final authRemoteDataSourceProvider = Provider<AuthRemoteDataSource>((ref) {
  return AuthRemoteDataSource(
    bareDio: ref.watch(bareDioProvider),
    accessTokenHolder: ref.watch(accessTokenHolderProvider),
  );
});

final authRepositoryProvider = Provider<AuthRepository>((ref) {
  return AuthRepositoryImpl(
    remote: ref.watch(authRemoteDataSourceProvider),
  );
});
