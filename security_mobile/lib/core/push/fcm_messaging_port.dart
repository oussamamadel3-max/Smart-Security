/// Abstraction over Firebase Messaging so MOB-T-06 client logic works without
/// shipping Service Account secrets or requiring a live device in this sequence.
abstract class FcmMessagingPort {
  /// Current registration token, or null when unavailable / permission denied.
  Future<String?> getToken();

  /// Fired when the platform rotates the FCM token.
  Stream<String> get onTokenRefresh;

  /// Cold-start / background tap payload (data map keys only).
  Future<Map<String, String>?> getInitialMessageData();

  /// Taps while app is in background/foreground (data map).
  Stream<Map<String, String>> get onNotificationOpened;

  /// Request OS notification permission. Returns false when denied.
  Future<bool> requestPermission();

  /// Messages received while the app is in the foreground
  /// (`FirebaseMessaging.onMessage`) — FCM-C-05.
  ///
  /// Additive to the contract above (MOB-T-06): the port had no hook for
  /// this case because `NoOpFcmMessaging` never received any message.
  /// Required to display an in-app alert for a message, since Android does
  /// not auto-show anything for a foreground data-only payload (see
  /// `FcmPayloadBuilder` — identifiers only, no `notification` block).
  Stream<Map<String, String>> get onForegroundMessage;
}

/// Default until Firebase client files exist for each flavor.
class NoOpFcmMessaging implements FcmMessagingPort {
  const NoOpFcmMessaging();

  @override
  Future<String?> getToken() async => null;

  @override
  Stream<String> get onTokenRefresh => const Stream.empty();

  @override
  Future<Map<String, String>?> getInitialMessageData() async => null;

  @override
  Stream<Map<String, String>> get onNotificationOpened => const Stream.empty();

  @override
  Future<bool> requestPermission() async => false;

  @override
  Stream<Map<String, String>> get onForegroundMessage => const Stream.empty();
}
