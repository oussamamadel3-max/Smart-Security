class MeUserDto {
  const MeUserDto({
    required this.id,
    this.userName,
    this.fullName,
    this.email,
  });

  final String id;
  final String? userName;
  final String? fullName;
  final String? email;

  factory MeUserDto.fromJson(Map<String, dynamic> json) {
    return MeUserDto(
      id: (json['id'] as String? ?? '').trim(),
      userName: (json['userName'] as String?)?.trim(),
      fullName: (json['fullName'] as String?)?.trim(),
      email: (json['email'] as String?)?.trim(),
    );
  }
}
