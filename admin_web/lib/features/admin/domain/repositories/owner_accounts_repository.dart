import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../create_owner_input.dart';
import '../entities/owner_account_entity.dart';

abstract class OwnerAccountsRepository {
  Future<Result<List<OwnerAccountEntity>>> list();

  Future<Result<OwnerAccountEntity>> getById(String ownerId);

  Future<Result<OwnerAccountEntity>> create(CreateOwnerInput input);

  Future<Result<OwnerAccountEntity>> update(
    String ownerId,
    UpdateOwnerInput input,
  );

  Future<Result<bool>> disable(String ownerId);

  Future<Result<bool>> archive(String ownerId);

  void clearCache();
}
