import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/push/push_token_id_store.dart';
import '../../domain/repositories/push_tokens_repository.dart';
import '../datasources/push_tokens_remote_datasource.dart';
import '../models/push_token_dto.dart';

class PushTokensRepositoryImpl implements PushTokensRepository {
  PushTokensRepositoryImpl(this._remote, this._idStore);

  final PushTokensRemoteDataSource _remote;
  final PushTokenIdStore _idStore;

  @override
  Future<Result<String>> register({
    required String token,
    required String platform,
  }) async {
    // Safe to retry on network/timeout/server failures: Backend
    // RegisterOrUpdateAsync is keyed by token fingerprint (upsert), so a
    // repeated call with the same token+user never creates a duplicate row.
    final result = await executeWithRetry(
      () => _remote.register(
        RegisterPushTokenRequestDto(token: token, platform: platform),
      ),
    );
    return switch (result) {
      Failure_(:final failure) => Failure_(failure),
      Success(:final data) => () async {
          await _idStore.write(data.pushTokenId);
          return Success(data.pushTokenId);
        }(),
    };
  }

  @override
  Future<Result<void>> revokeStoredOrAll() async {
    final storedId = await _idStore.read();
    // Safe to retry: Backend RevokeAsync/RevokeAllActiveAsync only set
    // revokedAt when still null, so a repeated call is a no-op, not a
    // duplicate side effect.
    final result = storedId != null && storedId.isNotEmpty
        ? await executeWithRetry(() => _remote.revokeById(storedId))
        : await executeWithRetry(_remote.revokeAll);
    return switch (result) {
      Success() => () async {
          // Only clear the locally stored id once the server confirms the
          // revoke — never claim revocation happened (and never orphan the
          // id) when a network interruption made it uncertain.
          await _idStore.clear();
          return const Success<void>(null);
        }(),
      Failure_(:final failure) => Failure_<void>(failure),
    };
  }

  @override
  Future<void> clearLocalId() => _idStore.clear();
}
