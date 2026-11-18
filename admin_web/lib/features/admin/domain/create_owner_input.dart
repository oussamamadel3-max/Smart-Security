/// Create-owner payload. [password] is omitted from [toString].
class CreateOwnerInput {
  const CreateOwnerInput({
    required this.userName,
    required this.fullName,
    required this.password,
    this.email,
  });

  final String userName;
  final String fullName;
  final String password;
  final String? email;

  @override
  String toString() =>
      'CreateOwnerInput(userName: $userName, fullName: $fullName)';
}

class UpdateOwnerInput {
  const UpdateOwnerInput({required this.fullName, this.email});

  final String fullName;
  final String? email;
}

/// Field keys returned to the form. Empty means the input is acceptable.
abstract final class OwnerAccountRules {
  static const userName = 'userName';
  static const fullName = 'fullName';
  static const password = 'password';
  static const email = 'email';

  static String? validateCreate({
    required String userName,
    required String fullName,
    required String password,
    String? email,
  }) {
    final name = userName.trim();
    if (name.isEmpty || name.length > 120) return OwnerAccountRules.userName;
    final full = fullName.trim();
    if (full.isEmpty || full.length > 200) return OwnerAccountRules.fullName;
    if (password.length < 8) return OwnerAccountRules.password;
    return _email(email);
  }

  static String? validateUpdate({required String fullName, String? email}) {
    final full = fullName.trim();
    if (full.isEmpty || full.length > 200) return OwnerAccountRules.fullName;
    return _email(email);
  }

  static String? _email(String? email) {
    final value = email?.trim() ?? '';
    if (value.isEmpty) return null;
    if (value.contains(' ') || !value.contains('@')) {
      return OwnerAccountRules.email;
    }
    return null;
  }
}
