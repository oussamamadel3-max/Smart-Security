class RegisterPushTokenRequestDto {
  const RegisterPushTokenRequestDto({
    required this.token,
    required this.platform,
  });

  final String token;
  final String platform;

  Map<String, dynamic> toJson() => {
        'token': token,
        'platform': platform,
      };
}

class PushTokenResponseDto {
  const PushTokenResponseDto({
    required this.pushTokenId,
    required this.platform,
    required this.registeredAt,
    required this.isActive,
    this.lastUsedAt,
  });

  final String pushTokenId;
  final String platform;
  final String registeredAt;
  final String? lastUsedAt;
  final bool isActive;

  factory PushTokenResponseDto.fromJson(Map<String, dynamic> json) {
    return PushTokenResponseDto(
      pushTokenId: (json['pushTokenId'] as String? ?? '').trim(),
      platform: (json['platform'] as String? ?? '').trim(),
      registeredAt: json['registeredAt']?.toString() ?? '',
      lastUsedAt: json['lastUsedAt']?.toString(),
      isActive: json['isActive'] as bool? ?? false,
    );
  }
}
