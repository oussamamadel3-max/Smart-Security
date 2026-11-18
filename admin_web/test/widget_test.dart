import 'package:admin_web/app.dart';
import 'package:admin_web/core/network/auth_dependency_providers.dart';
import 'package:admin_web/core/session/csrf_token_reader.dart';
import 'package:admin_web/features/auth/domain/entities/auth_session_tokens.dart';
import 'package:admin_web/features/auth/domain/entities/auth_user_entity.dart';
import 'package:admin_web/features/auth/domain/repositories/auth_repository.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

class _NoSessionRepo implements AuthRepository {
  @override
  Future<Result<AuthSessionTokens>> login({
    required String userName,
    required String password,
  }) async => const Failure_(Failure.unauthorized());

  @override
  Future<Result<bool>> logout() async => const Success(true);

  @override
  Future<Result<bool>> logoutAll() async => const Success(true);

  @override
  Future<Result<AuthUserEntity>> me() async =>
      const Failure_(Failure.unauthorized());

  @override
  Future<Result<AuthSessionTokens>> refresh() async =>
      const Failure_(Failure.unauthorized());
}

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  GoogleFonts.config.allowRuntimeFetching = false;

  testWidgets('web shell boots login screen after splash', (tester) async {
    SharedPreferences.setMockInitialValues({});
    final prefs = await SharedPreferences.getInstance();

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          appCoreConfigProvider.overrideWithValue(
            const AppCoreConfig(baseUrl: 'http://localhost'),
          ),
          sharedPreferencesProvider.overrideWithValue(prefs),
          csrfTokenReaderProvider.overrideWithValue(MemoryCsrfTokenReader()),
          authRepositoryProvider.overrideWithValue(_NoSessionRepo()),
        ],
        child: const HisnWebApp(),
      ),
    );
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 50));
    await tester.pumpAndSettle();

    expect(find.textContaining('حِصن'), findsWidgets);
    expect(find.text('تسجيل الدخول'), findsWidgets);
  });
}
