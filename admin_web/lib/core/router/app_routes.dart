abstract final class AppRoutes {
  static const splash = '/';
  static const login = '/login';
  static const ownerHome = '/owner';
  static const adminHome = '/admin';
  static const adminAccounts = '/admin/accounts';
  static const adminAccountNew = '/admin/accounts/new';
  static const adminAccountDetail = '/admin/accounts/:ownerId';
  static const adminDevices = '/admin/devices';
  static const adminDeviceRegister = '/admin/devices/register';
  static const adminDeviceDetail = '/admin/devices/:deviceId';
  static const adminDeviceLink = '/admin/devices/:deviceId/link';
  static const adminDeviceTransfer = '/admin/devices/:deviceId/transfer';
  static const adminAuditLogs = '/admin/audit-logs';
  static const adminAuditLogDetail = '/admin/audit-logs/:logId';
  static const adminAlerts = '/admin/alerts';
  static const adminAlertNew = '/admin/alerts/new';
  static const adminAlertDetail = '/admin/alerts/:alertId';

  static String adminAccountPath(String ownerId) => '/admin/accounts/$ownerId';

  static String adminDevicePath(String deviceId) => '/admin/devices/$deviceId';

  static String adminDeviceLinkPath(String deviceId) =>
      '/admin/devices/$deviceId/link';

  static String adminDeviceTransferPath(String deviceId) =>
      '/admin/devices/$deviceId/transfer';

  static String adminAuditLogPath(String logId) => '/admin/audit-logs/$logId';

  static String adminAlertPath(String alertId) => '/admin/alerts/$alertId';

  /// Owner device detail + commands. Not registered under admin routes.
  static const ownerDeviceDetail = '/owner/devices/:deviceId';

  static const ownerDeviceHistory = '/owner/devices/:deviceId/history';

  static const ownerDeviceAlarmEvent =
      '/owner/devices/:deviceId/history/events/:alarmEventId';

  static const ownerDeviceCommand =
      '/owner/devices/:deviceId/history/commands/:commandId';

  static String ownerDeviceDetailPath(String deviceId) =>
      '/owner/devices/$deviceId';

  static String ownerDeviceHistoryPath(
    String deviceId, {
    String tab = 'events',
  }) => '/owner/devices/$deviceId/history?tab=$tab';

  static String ownerDeviceAlarmEventPath(
    String deviceId,
    String alarmEventId,
  ) => '/owner/devices/$deviceId/history/events/$alarmEventId';

  static String ownerDeviceCommandPath(String deviceId, String commandId) =>
      '/owner/devices/$deviceId/history/commands/$commandId';
}
