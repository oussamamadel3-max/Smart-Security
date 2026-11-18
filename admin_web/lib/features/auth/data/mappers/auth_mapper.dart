import '../../../../core/session/jwt_claims_reader.dart';
import '../../domain/entities/auth_session_tokens.dart';
import '../../domain/entities/auth_user_entity.dart';
import '../models/auth_tokens_dto.dart';
import '../models/me_user_dto.dart';

abstract final class AuthMapper {
  static AuthUserEntity toUser(MeUserDto dto, {List<String> roles = const []}) {
    return AuthUserEntity(
      id: dto.id,
      userName: dto.userName,
      fullName: dto.fullName,
      email: dto.email,
      roles: roles,
    );
  }

  static AuthSessionTokens toSession(AuthTokensDto dto) {
    final roles = JwtClaimsReader.roles(dto.accessToken);
    return AuthSessionTokens(
      accessToken: dto.accessToken,
      expiresIn: dto.expiresIn,
      refreshChannel: dto.refreshChannel,
      user: toUser(dto.user, roles: roles),
    );
  }
}
