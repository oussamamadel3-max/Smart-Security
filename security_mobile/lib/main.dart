import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import 'app.dart';
import 'core/config/app_environment.dart';
import 'core/network/mobile_network_providers.dart';
import 'core/push/firebase_bootstrap.dart';
import 'core/push/firebase_messaging_background_handler.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await FirebaseBootstrap.ensureInitialized();
  if (FirebaseBootstrap.isInitialized) {
    FirebaseMessaging.onBackgroundMessage(firebaseMessagingBackgroundHandler);
  }
  final prefs = await SharedPreferences.getInstance();
  final env = AppEnvironment.current;

  runApp(
    ProviderScope(
      overrides: [
        appCoreConfigProvider.overrideWithValue(
          AppCoreConfig(baseUrl: env.apiBaseUrl),
        ),
        sharedPreferencesProvider.overrideWithValue(prefs),
        dioProvider.overrideWith((ref) => ref.watch(mobileDioProvider)),
      ],
      child: const HisnMobileApp(),
    ),
  );
}
