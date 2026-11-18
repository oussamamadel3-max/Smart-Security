import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/session/session_epoch.dart';
import '../../domain/create_owner_input.dart';
import '../../domain/entities/owner_account_entity.dart';
import '../../domain/repositories/owner_accounts_repository.dart';
import '../datasources/owner_accounts_remote_datasource.dart';
import '../mappers/admin_mapper.dart';

class OwnerAccountsRepositoryImpl implements OwnerAccountsRepository {
  OwnerAccountsRepositoryImpl(this._remote, {SessionEpoch? epoch})
    : _epoch = epoch ?? SessionEpoch();

  final OwnerAccountsRemoteDataSource _remote;
  final SessionEpoch _epoch;
  List<OwnerAccountEntity>? _listCache;

  bool _current(int captured) => _epoch.isCurrent(captured);

  @override
  Future<Result<List<OwnerAccountEntity>>> list() async {
    final captured = _epoch.capture();
    final result = await executeWithRetry(_remote.list);
    return switch (result) {
      Failure_(:final failure) => Failure_(failure),
      Success(:final data) => () {
        final entities = data.map(OwnerAccountMapper.toEntity).toList();
        if (_current(captured)) _listCache = entities;
        return Success(entities);
      }(),
    };
  }

  @override
  Future<Result<OwnerAccountEntity>> getById(String ownerId) async {
    final result = await executeWithRetry(() => _remote.getById(ownerId));
    return _one(result);
  }

  @override
  Future<Result<OwnerAccountEntity>> create(CreateOwnerInput input) async {
    final captured = _epoch.capture();
    final result = await _remote.create(input);
    if (result is Success && _current(captured)) _listCache = null;
    return _one(result);
  }

  @override
  Future<Result<OwnerAccountEntity>> update(
    String ownerId,
    UpdateOwnerInput input,
  ) async {
    final captured = _epoch.capture();
    final result = await _remote.update(ownerId, input);
    if (result is Success && _current(captured)) _listCache = null;
    return _one(result);
  }

  @override
  Future<Result<bool>> disable(String ownerId) async {
    final captured = _epoch.capture();
    final result = await _remote.disable(ownerId);
    if (result is Success && _current(captured)) _listCache = null;
    return result;
  }

  @override
  Future<Result<bool>> archive(String ownerId) async {
    final captured = _epoch.capture();
    final result = await _remote.archive(ownerId);
    if (result is Success && _current(captured)) _listCache = null;
    return result;
  }

  @override
  void clearCache() => _listCache = null;

  bool get hasCachedList => _listCache != null;

  Result<OwnerAccountEntity> _one(Result<Map<String, dynamic>> result) {
    return switch (result) {
      Failure_(:final failure) => Failure_(failure),
      Success(:final data) => Success(OwnerAccountMapper.toEntity(data)),
    };
  }
}
