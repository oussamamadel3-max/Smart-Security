/// Semantic status codes aligned with Backend `DbStatusCodes`.
///
/// Unknown wire values must never be treated as success — use [Unknown] /
/// [parseOrUnknown] helpers. These models are shared meaning with Web; they
/// are not an authorization substitute for server checks.
library;

enum ConnectionStatus {
  online('ONLINE'),
  offline('OFFLINE'),
  unknown('UNKNOWN');

  const ConnectionStatus(this.code);
  final String code;

  static ConnectionStatus parse(String? raw) {
    final value = raw?.trim().toUpperCase();
    for (final item in values) {
      if (item != unknown && item.code == value) return item;
    }
    return unknown;
  }

  bool get isOnline => this == online;
}

enum OwnershipStatus {
  pending('PENDING'),
  active('ACTIVE'),
  ended('ENDED'),
  cancelled('CANCELLED'),
  unknown('UNKNOWN');

  const OwnershipStatus(this.code);
  final String code;

  static OwnershipStatus parse(String? raw) {
    final value = raw?.trim().toUpperCase();
    for (final item in values) {
      if (item != unknown && item.code == value) return item;
    }
    return unknown;
  }

  bool get isUsableForCommands => this == active;
  bool get isLinkPending => this == pending;
}

enum ArmStatus {
  armed('ARMED'),
  disarmed('DISARMED'),
  unknown('UNKNOWN');

  const ArmStatus(this.code);
  final String code;

  static ArmStatus parse(String? raw) {
    final value = raw?.trim().toUpperCase();
    for (final item in values) {
      if (item.code == value) return item;
    }
    return unknown;
  }
}

enum SirenStatus {
  off('OFF'),
  alarm('ALARM'),
  test('TEST'),
  unknown('UNKNOWN');

  const SirenStatus(this.code);
  final String code;

  static SirenStatus parse(String? raw) {
    final value = raw?.trim().toUpperCase();
    for (final item in values) {
      if (item.code == value) return item;
    }
    return unknown;
  }
}

enum DiagnosticStatus {
  healthy('HEALTHY'),
  fault('FAULT'),
  unknown('UNKNOWN');

  const DiagnosticStatus(this.code);
  final String code;

  static DiagnosticStatus parse(String? raw) {
    final value = raw?.trim().toUpperCase();
    for (final item in values) {
      if (item.code == value) return item;
    }
    return unknown;
  }
}

enum CommandType {
  arm('ARM'),
  disarm('DISARM'),
  muteAlarm('MUTE_ALARM'),
  testSiren('TEST_SIREN'),
  stopTest('STOP_TEST'),
  unknown('UNKNOWN');

  const CommandType(this.code);
  final String code;

  static CommandType parse(String? raw) {
    final value = raw?.trim().toUpperCase();
    for (final item in values) {
      if (item != unknown && item.code == value) return item;
    }
    return unknown;
  }
}

enum CommandStatus {
  pending('PENDING'),
  executed('EXECUTED'),
  rejected('REJECTED'),
  timedOut('TIMED_OUT'),
  cancelled('CANCELLED'),
  unknown('UNKNOWN');

  const CommandStatus(this.code);
  final String code;

  static CommandStatus parse(String? raw) {
    final value = raw?.trim().toUpperCase();
    for (final item in values) {
      if (item != unknown && item.code == value) return item;
    }
    return unknown;
  }

  /// HTTP 202 / PENDING is not device execution.
  bool get isConfirmedExecution => this == executed;

  /// TIMED_OUT is an unknown outcome until reconciliation.
  bool get isUnresolvedTimeout => this == timedOut;
}

enum AlarmEventStatus {
  active('ACTIVE'),
  ended('ENDED'),
  unknown('UNKNOWN');

  const AlarmEventStatus(this.code);
  final String code;

  static AlarmEventStatus parse(String? raw) {
    final value = raw?.trim().toUpperCase();
    for (final item in values) {
      if (item != unknown && item.code == value) return item;
    }
    return unknown;
  }
}

/// Owner app role code as issued by Backend Identity.
abstract final class AppRoleCodes {
  static const owner = 'OWNER';
  static const admin = 'ADMIN';
}
