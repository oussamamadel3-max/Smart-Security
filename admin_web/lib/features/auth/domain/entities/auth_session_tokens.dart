import 'auth_user_entity.dart';

class AuthSessionTokens {
  const AuthSessionTokens({
    required this.accessToken,
    required this.expiresIn,
    required this.refreshChannel,
    required this.user,
  });

  final String accessToken;
  final int expiresIn;

  /// Backend channel marker (`COOKIE` for web).
  final String refreshChannel;
  final AuthUserEntity user;
}
