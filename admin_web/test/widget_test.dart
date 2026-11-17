import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import 'package:admin_web/app.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  GoogleFonts.config.allowRuntimeFetching = false;

  testWidgets('web shell boots shared core theme and login placeholders',
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
        ],
        child: const HisnWebApp(),
      ),
    );
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 350));
    await tester.pumpAndSettle();

    expect(find.textContaining('حِصن'), findsWidgets);
    expect(find.text('معاينة مسار المالك'), findsOneWidget);
    expect(find.text('معاينة مسار الأدمن'), findsOneWidget);
  });
}
