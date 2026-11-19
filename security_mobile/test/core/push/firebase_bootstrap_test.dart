import 'package:flutter_test/flutter_test.dart';
import 'package:security_mobile/core/push/firebase_bootstrap.dart';

void main() {
  // This suite runs on the host (Windows CI/dev machine), never Android, so
  // `isSupportedPlatform` is always false here — that is the behavior under
  // test, not a limitation. Android-only initialization success/failure
  // needs a real or emulated Android process and is out of scope for this
  // stage (no device/emulator — FCM-C-03 constraint).
  setUp(FirebaseBootstrap.resetForTest);
  tearDown(FirebaseBootstrap.resetForTest);

  test('is not initialized before ensureInitialized runs', () {
    expect(FirebaseBootstrap.isInitialized, isFalse);
  });

  test('unsupported platform (this host): ensureInitialized is a no-op '
      'and never throws', () async {
    expect(FirebaseBootstrap.isSupportedPlatform, isFalse);
    await FirebaseBootstrap.ensureInitialized();
    expect(FirebaseBootstrap.isInitialized, isFalse);
  });

  test('calling ensureInitialized twice does not attempt init twice', () async {
    await FirebaseBootstrap.ensureInitialized();
    final afterFirst = FirebaseBootstrap.isInitialized;
    await FirebaseBootstrap.ensureInitialized();
    expect(FirebaseBootstrap.isInitialized, afterFirst);
  });

  test('resetForTest clears the attempted/initialized flags', () async {
    await FirebaseBootstrap.ensureInitialized();
    FirebaseBootstrap.resetForTest();
    expect(FirebaseBootstrap.isInitialized, isFalse);
  });
}
