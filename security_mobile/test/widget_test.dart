import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import 'package:security_mobile/app.dart';
import 'package:security_mobile/core/network/auth_dependency_providers.dart';
import 'package:security_mobile/core/session/refresh_token_store.dart';

class _EmptyRefreshStore implements RefreshTokenStore {
  @override
  Future<void> clear() async {}

  @override
  Future<String?> read() async => null;

  @override
  Future<void> write(String token) async {}
}

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  GoogleFonts.config.allowRuntimeFetching = false;

  testWidgets('mobile shell shows login brand after splash restore',
      (tester) async {
    SharedPreferences.setMockInitialValues({});
    final prefs = await SharedPreferences.getInstance();

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          appCoreConfigProvider.overrideWithValue(
            const AppCoreConfig(baseUrl: 'http://localhost'),
          ),
          sharedPreferencesProvider.overrideWithValue(prefs),
          refreshTokenStoreProvider.overrideWithValue(_EmptyRefreshStore()),
        ],
        child: const HisnMobileApp(),
      ),
    );
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));
    await tester.pumpAndSettle();

    expect(find.textContaining('حِصن'), findsWidgets);
    expect(find.textContaining('مرحباً بعودتك'), findsOneWidget);
  });
}
