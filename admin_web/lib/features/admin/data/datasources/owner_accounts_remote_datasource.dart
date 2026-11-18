import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/network/app_endpoints.dart';
import '../../domain/create_owner_input.dart';
import '../mappers/admin_json.dart';

class OwnerAccountsRemoteDataSource extends ApiClient {
  OwnerAccountsRemoteDataSource(super.dio);

  Future<Result<List<Map<String, dynamic>>>> list() {
    return get(AppEndpoints.adminOwners, parser: readMapList);
  }

  Future<Result<Map<String, dynamic>>> getById(String ownerId) {
    return get(AppEndpoints.adminOwner(ownerId), parser: readMap);
  }

  Future<Result<Map<String, dynamic>>> create(CreateOwnerInput input) {
    return post(
      AppEndpoints.adminOwners,
      data: {
        'userName': input.userName.trim(),
        'fullName': input.fullName.trim(),
        'password': input.password,
        'email': _email(input.email),
      },
      parser: readMap,
    );
  }

  Future<Result<Map<String, dynamic>>> update(
    String ownerId,
    UpdateOwnerInput input,
  ) {
    return put(
      AppEndpoints.adminOwner(ownerId),
      data: {'fullName': input.fullName.trim(), 'email': _email(input.email)},
      parser: readMap,
    );
  }

  Future<Result<bool>> disable(String ownerId) {
    return post(AppEndpoints.adminOwnerDisable(ownerId), parser: (_) => true);
  }

  Future<Result<bool>> archive(String ownerId) {
    return post(AppEndpoints.adminOwnerArchive(ownerId), parser: (_) => true);
  }
}

String? _email(String? email) {
  final value = email?.trim() ?? '';
  return value.isEmpty ? null : value;
}
