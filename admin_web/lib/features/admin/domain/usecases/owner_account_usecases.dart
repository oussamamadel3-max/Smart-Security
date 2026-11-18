import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../create_owner_input.dart';
import '../entities/owner_account_entity.dart';
import '../repositories/owner_accounts_repository.dart';

class ListOwnerAccountsUseCase {
  const ListOwnerAccountsUseCase(this._repository);

  final OwnerAccountsRepository _repository;

  Future<Result<List<OwnerAccountEntity>>> call() => _repository.list();
}

class GetOwnerAccountUseCase {
  const GetOwnerAccountUseCase(this._repository);

  final OwnerAccountsRepository _repository;

  Future<Result<OwnerAccountEntity>> call(String ownerId) =>
      _repository.getById(ownerId);
}

class CreateOwnerAccountUseCase {
  const CreateOwnerAccountUseCase(this._repository);

  final OwnerAccountsRepository _repository;

  Future<Result<OwnerAccountEntity>> call(CreateOwnerInput input) =>
      _repository.create(input);
}

class UpdateOwnerAccountUseCase {
  const UpdateOwnerAccountUseCase(this._repository);

  final OwnerAccountsRepository _repository;

  Future<Result<OwnerAccountEntity>> call(
    String ownerId,
    UpdateOwnerInput input,
  ) => _repository.update(ownerId, input);
}

class DisableOwnerAccountUseCase {
  const DisableOwnerAccountUseCase(this._repository);

  final OwnerAccountsRepository _repository;

  Future<Result<bool>> call(String ownerId) => _repository.disable(ownerId);
}

class ArchiveOwnerAccountUseCase {
  const ArchiveOwnerAccountUseCase(this._repository);

  final OwnerAccountsRepository _repository;

  Future<Result<bool>> call(String ownerId) => _repository.archive(ownerId);
}
