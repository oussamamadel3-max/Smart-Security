import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import 'app.dart';
import 'core/config/app_environment.dart';
import 'core/network/web_credentials_adapter.dart';
import 'core/network/web_network_providers.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  final prefs = await SharedPreferences.getInstance();
  final env = AppEnvironment.current;

  runApp(
    ProviderScope(
      overrides: [
        appCoreConfigProvider.overrideWithValue(
          AppCoreConfig(
            baseUrl: env.apiBaseUrl,
            httpClientAdapter: createWebCredentialsAdapter(),
          ),
        ),
        sharedPreferencesProvider.overrideWithValue(prefs),
        dioProvider.overrideWith((ref) => ref.watch(webDioProvider)),
      ],
      child: const HisnWebApp(),
    ),
  );
}
