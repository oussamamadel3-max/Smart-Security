import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:security_mobile/core/push/fcm_messaging_port.dart';
import 'package:security_mobile/features/notifications/presentation/providers/notifications_providers.dart';

void main() {
  test(
    'fcmMessagingPortProvider resolves to NoOpFcmMessaging on an '
    'unsupported platform (this host has no Android runtime)',
    () {
      final container = ProviderContainer();
      addTearDown(container.dispose);

      final port = container.read(fcmMessagingPortProvider);

      expect(port, isA<NoOpFcmMessaging>());
    },
  );
}
