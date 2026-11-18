abstract final class AppRoutes {
  static const splash = '/';
  static const login = '/login';

  /// Post-auth landing (dashboard tab).
  static const home = '/app/dashboard';

  static const dashboard = '/app/dashboard';
  static const devices = '/app/devices';
  static const notifications = '/app/notifications';
  static const profile = '/app/profile';

  static const deviceDetail = '/devices/:deviceId';
  static const deviceHistory = '/devices/:deviceId/history';
  static const deviceAlarmEventDetail =
      '/devices/:deviceId/history/events/:alarmEventId';
  static const deviceCommandDetail =
      '/devices/:deviceId/history/commands/:commandId';
  static const deviceWifiSetup = '/devices/:deviceId/wifi';

  static String deviceDetailPath(String deviceId) => '/devices/$deviceId';

  static String deviceWifiSetupPath(String deviceId) =>
      '/devices/$deviceId/wifi';

  static String deviceHistoryPath(String deviceId, {String tab = 'events'}) =>
      '/devices/$deviceId/history?tab=$tab';

  static String deviceAlarmEventDetailPath(
    String deviceId,
    String alarmEventId,
  ) =>
      '/devices/$deviceId/history/events/$alarmEventId';

  static String deviceCommandDetailPath(String deviceId, String commandId) =>
      '/devices/$deviceId/history/commands/$commandId';
}
