import 'package:smart_security_app_core/smart_security_app_core.dart';

/// Mobile endpoint paths. Auth paths mirror Backend T-02.
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

  static String ownerDeviceAlarmEvents(String deviceId) =>
      '${ownerDevice(deviceId)}/alarm-events';

  static String ownerDeviceAlarmEvent(String deviceId, String alarmEventId) =>
      '${ownerDeviceAlarmEvents(deviceId)}/$alarmEventId';

  static const pushTokens = '/api/me/push-tokens';

  static String pushToken(String pushTokenId) => '$pushTokens/$pushTokenId';

  static const ownerNotifications = '/api/owner/notifications';

  static String ownerNotification(String notificationId) =>
      '$ownerNotifications/$notificationId';
}
