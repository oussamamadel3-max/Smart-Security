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
    final result = await _remote.register(
      RegisterPushTokenRequestDto(token: token, platform: platform),
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
    final result = storedId != null && storedId.isNotEmpty
        ? await _remote.revokeById(storedId)
        : await _remote.revokeAll();
    await _idStore.clear();
    return switch (result) {
      Success() => const Success<void>(null),
      Failure_(:final failure) => Failure_<void>(failure),
    };
  }

  @override
  Future<void> clearLocalId() => _idStore.clear();
}
