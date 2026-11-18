import 'package:smart_security_app_core/smart_security_app_core.dart';

class AuthUserEntity {
  const AuthUserEntity({
    required this.id,
    this.userName,
    this.fullName,
    this.email,
    this.roles = const [],
  });

  final String id;
  final String? userName;
  final String? fullName;
  final String? email;
  final List<String> roles;

  bool get isOwner => roles.any((r) => r.toUpperCase() == AppRoleCodes.owner);

  bool get isAdmin => roles.any((r) => r.toUpperCase() == AppRoleCodes.admin);
}
