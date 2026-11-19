import 'package:flutter/foundation.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';

/// Posts a minimal, non-sensitive local notification for FCM payloads —
/// FCM-C-05.
///
/// `FcmPayloadBuilder` (Backend) sends identifier-only **data** messages
/// with no `notification` block (FCM-05: "حمولة إشعار قليلة البيانات...
/// تفاصيلها من API"). Android never auto-displays a data-only message in
/// any app state, so without this service a background/terminated alert
/// would be completely silent. The displayed text is deliberately generic
/// — never the notification type, device, or alarm identifiers — so no
/// sensitive detail ever reaches the lock screen; the real identifiers
/// travel only in [payload] for in-app routing once the user opens it.
///
/// Must be initialized once per Dart isolate (including the background
/// isolate used by `firebaseMessagingBackgroundHandler`) before first use.
class LocalNotificationsService {
  LocalNotificationsService._();

  static final LocalNotificationsService instance =
      LocalNotificationsService._();

  static const _channelId = 'hisn_security_alerts';
  static const _channelName = 'تنبيهات الأمان';
  static const _channelDescription = 'إشعارات نظام حِصن للأمان الذكي';

  final _plugin = FlutterLocalNotificationsPlugin();
  bool _initialized = false;

  bool get isInitialized => _initialized;

  /// [onTap] fires for a tap while this isolate's plugin instance is alive
  /// (foreground, or resumed from background with the engine still running).
  /// It does NOT cover the terminated cold-start case — use
  /// [consumeLaunchPayload] for that, once, right after [initialize].
  Future<void> initialize({void Function(String? payload)? onTap}) async {
    if (_initialized) return;
    try {
      const androidInit = AndroidInitializationSettings(
        '@mipmap/ic_launcher',
      );
      await _plugin.initialize(
        settings: const InitializationSettings(android: androidInit),
        onDidReceiveNotificationResponse: (response) {
          onTap?.call(response.payload);
        },
        onDidReceiveBackgroundNotificationResponse: _backgroundTapNoOp,
      );
      const channel = AndroidNotificationChannel(
        _channelId,
        _channelName,
        description: _channelDescription,
        importance: Importance.high,
      );
      await _plugin
          .resolvePlatformSpecificImplementation<
            AndroidFlutterLocalNotificationsPlugin
          >()
          ?.createNotificationChannel(channel);
      _initialized = true;
    } catch (error) {
      // Never crash app/background-isolate startup over a display concern.
      debugPrint('LocalNotificationsService.initialize failed: $error');
      _initialized = false;
    }
  }

  /// Shows the generic alert. [payload] carries only the opaque identifier
  /// map already serialized by the caller (see
  /// `NotificationOpenCoordinator`) — never shown as visible text.
  Future<void> showGenericAlert({required String payload}) async {
    if (!_initialized) return;
    try {
      await _plugin.show(
        id: payload.hashCode,
        title: 'حِصن',
        body: 'لديك تنبيه جديد — افتح التطبيق للتفاصيل',
        notificationDetails: const NotificationDetails(
          android: AndroidNotificationDetails(
            _channelId,
            _channelName,
            channelDescription: _channelDescription,
            importance: Importance.high,
            priority: Priority.high,
          ),
        ),
        payload: payload,
      );
    } catch (error) {
      debugPrint('LocalNotificationsService.showGenericAlert failed: $error');
    }
  }

  /// Returns the payload that launched the app from a terminated state by
  /// tapping one of **our own** local notifications, or null otherwise.
  /// Call at most once per process — the plugin keeps no "already
  /// consumed" guard of its own.
  Future<String?> consumeLaunchPayload() async {
    try {
      final details = await _plugin.getNotificationAppLaunchDetails();
      if (details == null || !details.didNotificationLaunchApp) return null;
      return details.notificationResponse?.payload;
    } catch (error) {
      debugPrint(
        'LocalNotificationsService.consumeLaunchPayload failed: $error',
      );
      return null;
    }
  }
}

/// Registered because the plugin requires a handler, but intentionally a
/// no-op: a tap while the Flutter engine is not already running relaunches
/// the app, where [LocalNotificationsService.consumeLaunchPayload] (cold
/// start) or the live `onDidReceiveNotificationResponse` callback (engine
/// resumed) take over — never touch UI/BuildContext from this isolate.
@pragma('vm:entry-point')
void _backgroundTapNoOp(NotificationResponse response) {}
