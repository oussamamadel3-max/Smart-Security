import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import 'app.dart';
import 'core/config/app_environment.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  final prefs = await SharedPreferences.getInstance();
  final env = AppEnvironment.current;

  runApp(
    ProviderScope(
      overrides: [
        appCoreConfigProvider.overrideWithValue(
          AppCoreConfig(baseUrl: env.apiBaseUrl),
        ),
        sharedPreferencesProvider.overrideWithValue(prefs),
      ],
      child: const HisnMobileApp(),
    ),
  );
}
