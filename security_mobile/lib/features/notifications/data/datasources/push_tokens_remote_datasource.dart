import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/network/app_endpoints.dart';
import '../models/push_token_dto.dart';

class PushTokensRemoteDataSource extends ApiClient {
  PushTokensRemoteDataSource(super.dio);

  Future<Result<PushTokenResponseDto>> register(
    RegisterPushTokenRequestDto request,
  ) {
    return post(
      AppEndpoints.pushTokens,
      data: request.toJson(),
      parser: (json) => PushTokenResponseDto.fromJson(_asMap(json)),
    );
  }

  Future<Result<bool>> revokeById(String pushTokenId) {
    return delete(
      AppEndpoints.pushToken(pushTokenId),
      parser: (_) => true,
    );
  }

  Future<Result<bool>> revokeAll() {
    return delete(
      AppEndpoints.pushTokens,
      parser: (_) => true,
    );
  }

  Map<String, dynamic> _asMap(dynamic json) {
    if (json is Map<String, dynamic>) return json;
    if (json is Map) return Map<String, dynamic>.from(json);
    throw const FormatException('Expected JSON object');
  }
}
