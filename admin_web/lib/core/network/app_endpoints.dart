import 'package:smart_security_app_core/smart_security_app_core.dart';

/// Web endpoint paths. Auth paths mirror Backend T-02.
abstract final class AppEndpoints {
  static const login = DefaultAuthPaths.login;
  static const refresh = DefaultAuthPaths.refresh;
  static const logout = DefaultAuthPaths.logout;
  static const logoutAll = DefaultAuthPaths.logoutAll;
  static const me = DefaultAuthPaths.me;

  /// Owner device list/detail (Backend owner query API).
  static const ownerDevices = '/api/owner/devices';

  static String ownerDevice(String deviceId) => '$ownerDevices/$deviceId';

  static String ownerDeviceCommands(String deviceId) =>
      '${ownerDevice(deviceId)}/commands';

  static String ownerDeviceCommand(String deviceId, String commandId) =>
      '${ownerDeviceCommands(deviceId)}/$commandId';

  /// Full ownership-period list. The controller accepts no page or search query.
  static String ownerDeviceAlarmEvents(String deviceId) =>
      '${ownerDevice(deviceId)}/alarm-events';

  static String ownerDeviceAlarmEvent(String deviceId, String alarmEventId) =>
      '${ownerDeviceAlarmEvents(deviceId)}/$alarmEventId';

  /// Admin owner accounts (Backend `OwnersController`).
  static const adminOwners = '/api/admin/owners';

  static String adminOwner(String ownerId) => '$adminOwners/$ownerId';

  static String adminOwnerDisable(String ownerId) =>
      '${adminOwner(ownerId)}/disable';

  static String adminOwnerArchive(String ownerId) =>
      '${adminOwner(ownerId)}/archive';

  /// Admin devices (Backend `DevicesController`). Read-only overview plus
  /// register / link / transfer / unlink / archive. No owner commands.
  static const adminDevices = '/api/admin/devices';

  static String adminDevice(String deviceId) => '$adminDevices/$deviceId';

  static String adminDeviceLink(String deviceId) =>
      '${adminDevice(deviceId)}/link';

  static String adminDeviceTransfer(String deviceId) =>
      '${adminDevice(deviceId)}/transfer';

  static String adminDeviceUnlink(String deviceId) =>
      '${adminDevice(deviceId)}/unlink';

  static String adminDeviceArchive(String deviceId) =>
      '${adminDevice(deviceId)}/archive';

  /// Read-only admin audit log (`AuditLogsController`).
  static const adminAuditLogs = '/api/admin/audit-logs';

  /// Admin alerts (`AdminAlertsController`). Create is not an FCM call.
  static const adminAlerts = '/api/admin/alerts';

  static String adminAlert(String alertId) => '$adminAlerts/$alertId';
}
