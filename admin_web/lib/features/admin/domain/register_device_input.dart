import 'dart:convert';

/// Registration input. [credentialValue] is omitted from [toString].
///
/// Capacities are caller-supplied. This type does not invent D-03 defaults.
class RegisterDeviceInput {
  const RegisterDeviceInput({
    required this.hardwareUid,
    required this.displayName,
    required this.authType,
    required this.credentialValue,
    required this.alarmRuleJson,
    required this.eventBufferCapacity,
    required this.commandTtlSeconds,
  });

  final String hardwareUid;
  final String displayName;
  final String authType;
  final String credentialValue;
  final String alarmRuleJson;
  final int eventBufferCapacity;
  final int commandTtlSeconds;

  @override
  String toString() =>
      'RegisterDeviceInput(hardwareUid: $hardwareUid, authType: $authType)';
}

abstract final class DeviceCredentialTypes {
  static const passwordHash = 'PASSWORD_HASH';
  static const certificate = 'CERTIFICATE';
  static const secretRef = 'SECRET_REF';

  static const values = [passwordHash, certificate, secretRef];
}

abstract final class RegisterDeviceRules {
  static const hardwareUid = 'hardwareUid';
  static const displayName = 'displayName';
  static const authType = 'authType';
  static const credential = 'credential';
  static const alarmRule = 'alarmRule';
  static const eventBufferCapacity = 'eventBufferCapacity';
  static const commandTtlSeconds = 'commandTtlSeconds';

  static String? validate(RegisterDeviceInput input) {
    final uid = input.hardwareUid.trim();
    if (uid.isEmpty || uid.length > 128) return hardwareUid;
    final name = input.displayName.trim();
    if (name.isEmpty || name.length > 150) return displayName;
    if (!DeviceCredentialTypes.values.contains(input.authType)) {
      return authType;
    }
    final secret = input.credentialValue;
    final secretError = switch (input.authType) {
      DeviceCredentialTypes.passwordHash =>
        secret.length < 8 || secret.length > 256,
      DeviceCredentialTypes.certificate =>
        secret.trim().length < 8 || secret.trim().length > 128,
      DeviceCredentialTypes.secretRef =>
        secret.trim().isEmpty || secret.trim().length > 512,
      _ => true,
    };
    if (secretError) return credential;
    if (!_isJsonObject(input.alarmRuleJson)) return alarmRule;
    if (input.eventBufferCapacity <= 0) return eventBufferCapacity;
    if (input.commandTtlSeconds <= 0) return commandTtlSeconds;
    return null;
  }

  static bool _isJsonObject(String raw) {
    try {
      final decoded = jsonDecode(raw);
      return decoded is Map<String, dynamic> || decoded is Map;
    } catch (_) {
      return false;
    }
  }
}
