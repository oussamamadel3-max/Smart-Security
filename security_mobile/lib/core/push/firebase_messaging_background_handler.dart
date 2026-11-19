import 'dart:convert';

import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';

import 'local_notifications_service.dart';

/// Background-isolate entrypoint registered via
/// `FirebaseMessaging.onBackgroundMessage` — FCM-C-05 constraint.
///
/// Runs in its own isolate with no access to the widget tree, Navigator, or
/// any state created in `main()`, so it must stay free of UI/BuildContext
/// work. Firebase is re-initialized here because background isolates do not
/// share the `main()` isolate's `Firebase.initializeApp()` call.
///
/// `FcmPayloadBuilder` sends data-only messages (no `notification` block —
/// FCM-05), so Android will NOT show anything on its own here. This handler
/// posts the generic local alert itself; [LocalNotificationsService] must be
/// (re-)initialized in this isolate too, since isolates do not share plugin
/// state with `main()`. Tap navigation is handled exclusively in the main
/// isolate (`NotificationOpenCoordinator`), never from this isolate.
@pragma('vm:entry-point')
Future<void> firebaseMessagingBackgroundHandler(RemoteMessage message) async {
  if (Firebase.apps.isEmpty) {
    await Firebase.initializeApp();
  }
  if (message.data.isEmpty) return;

  await LocalNotificationsService.instance.initialize();
  await LocalNotificationsService.instance.showGenericAlert(
    payload: jsonEncode(message.data),
  );
}
