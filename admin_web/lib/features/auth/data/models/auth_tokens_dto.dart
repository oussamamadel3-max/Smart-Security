import 'me_user_dto.dart';

class AuthTokensDto {
  const AuthTokensDto({
    required this.accessToken,
    required this.tokenType,
    required this.expiresIn,
    required this.refreshChannel,
    required this.user,
    this.refreshToken,
  });

  final String accessToken;
  final String tokenType;
  final int expiresIn;

  /// Null on cookie channel (stripped by server before response).
  final String? refreshToken;
  final String refreshChannel;
  final MeUserDto user;

  factory AuthTokensDto.fromJson(Map<String, dynamic> json) {
    final userRaw = json['user'];
    return AuthTokensDto(
      accessToken: (json['accessToken'] as String? ?? '').trim(),
      tokenType: (json['tokenType'] as String? ?? 'Bearer').trim(),
      expiresIn: (json['expiresIn'] as num?)?.toInt() ?? 0,
      refreshToken: (json['refreshToken'] as String?)?.trim(),
      refreshChannel: (json['refreshChannel'] as String? ?? '').trim(),
      user: userRaw is Map
          ? MeUserDto.fromJson(Map<String, dynamic>.from(userRaw))
          : const MeUserDto(id: ''),
    );
  }
}
