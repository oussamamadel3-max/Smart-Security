import 'auth_user_entity.dart';

class AuthSessionTokens {
  const AuthSessionTokens({
    required this.accessToken,
    required this.refreshToken,
    required this.expiresIn,
    required this.refreshChannel,
    required this.user,
  });

  final String accessToken;
  final String refreshToken;
  final int expiresIn;
  final String refreshChannel;
  final AuthUserEntity user;
}
