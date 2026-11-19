import 'dart:io' show Platform;

import 'package:firebase_core/firebase_core.dart';
import 'package:flutter/foundation.dart';

/// Idempotent Firebase initialization gate — FCM-C-03.
///
/// Only Android is configured in this stage: `google-services.json` exists
/// per flavor under `android/app/src/<flavor>/`. iOS has no
/// `GoogleService-Info.plist` yet (deferred, see
/// `security_mobile/mobile-remaining-tasks.md` R-04), so Firebase is never
/// initialized there — calling `Firebase.initializeApp()` without platform
/// options would throw on every iOS launch.
class FirebaseBootstrap {
  const FirebaseBootstrap._();

  static bool _attempted = false;
  static bool _initialized = false;

  /// True once `Firebase.initializeApp()` has completed successfully on a
  /// supported platform in this process.
  static bool get isInitialized => _initialized;

  static bool get isSupportedPlatform => !kIsWeb && Platform.isAndroid;

  /// Runs Firebase init at most once per process. `FcmMessagingPort`
  /// resolution never depends on [isInitialized] to decide between the real
  /// implementation and [NoOpFcmMessaging] — only [isSupportedPlatform]
  /// does, so an init failure on Android stays visible in logs from the
  /// real implementation instead of silently downgrading to NoOp.
  static Future<void> ensureInitialized() async {
    if (_attempted) return;
    _attempted = true;

    if (!isSupportedPlatform) {
      debugPrint(
        'FirebaseBootstrap: skipped — platform not configured in this stage.',
      );
      return;
    }

    try {
      if (Firebase.apps.isEmpty) {
        await Firebase.initializeApp();
      }
      _initialized = true;
    } catch (error) {
      debugPrint('FirebaseBootstrap: initialization failed: $error');
      _initialized = false;
    }
  }

  /// Test-only: lets widget/unit tests reset the gate between runs.
  @visibleForTesting
  static void resetForTest() {
    _attempted = false;
    _initialized = false;
  }
}
