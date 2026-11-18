import 'package:admin_web/app.dart';
import 'package:admin_web/core/router/app_router.dart';
import 'package:admin_web/core/router/app_routes.dart';
import 'package:admin_web/core/session/auth_session_controller.dart';
import 'package:admin_web/core/session/auth_session_state.dart';
import 'package:admin_web/features/admin/domain/entities/admin_device_entity.dart';
import 'package:admin_web/features/admin/domain/entities/admin_device_indicators_entity.dart';
import 'package:admin_web/features/admin/domain/entities/owner_account_entity.dart';
import 'package:admin_web/features/admin/presentation/admin_copy.dart';
import 'package:admin_web/features/admin/presentation/pages/admin_dashboard_page.dart';
import 'package:admin_web/features/admin/presentation/pages/create_owner_page.dart';
import 'package:admin_web/features/admin/presentation/viewmodels/admin_devices_view_model.dart';
import 'package:admin_web/features/admin/presentation/viewmodels/owner_accounts_view_model.dart';
import 'package:admin_web/features/admin/presentation/widgets/admin_shell.dart';
import 'package:admin_web/features/admin/presentation/widgets/admin_widgets.dart';
import 'package:admin_web/features/auth/domain/entities/auth_user_entity.dart';
import 'package:admin_web/features/auth/presentation/pages/login_page.dart';
import 'package:admin_web/features/devices/presentation/pages/device_history_page.dart';
import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  GoogleFonts.config.allowRuntimeFetching = false;

  for (final width in [360.0, 390.0, 768.0, 1024.0, 1440.0, 1920.0]) {
    testWidgets('admin dashboard fits $width without overflow', (tester) async {
      await _pumpDashboard(tester, width: width, height: 900);
      expect(tester.takeException(), isNull);
      expect(find.textContaining(AdminCopy.registerDevice), findsWidgets);
      final scaffold = tester.widget<Scaffold>(find.byType(Scaffold).first);
      if (width >= 960) {
        expect(scaffold.drawer, isNull);
        expect(find.text('حِصن'), findsOneWidget);
      } else {
        expect(scaffold.drawer, isNotNull);
        await tester.tap(find.byTooltip('القائمة'));
        await tester.pumpAndSettle();
        expect(find.text('حِصن'), findsOneWidget);
        expect(tester.takeException(), isNull);
      }
      _expectOnScreen(
        tester,
        find.textContaining(AdminCopy.registerDevice).first,
      );
    });
  }

  testWidgets('admin dashboard survives 1.5 text scale at 360', (tester) async {
    await _pumpDashboard(tester, width: 360, height: 800, textScale: 1.5);
    expect(tester.takeException(), isNull);
    _expectOnScreen(
      tester,
      find.textContaining(AdminCopy.registerDevice).first,
    );
  });

  testWidgets('long table ids scroll instead of overflowing at 360', (
    tester,
  ) async {
    await _setSurface(tester, 360, 700);
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: AdminTable(
            headers: ['المعرف', 'الحالة'],
            rows: [
              [Text('123e4567-e89b-12d3-a456-426614174000'), Text('نشط')],
            ],
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
  });

  testWidgets('confirm dialog stays inside a 360x640 viewport', (tester) async {
    await _setSurface(tester, 360, 640);
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: Builder(
            builder: (context) {
              return TextButton(
                onPressed: () => showAdminConfirmDialog(
                  context: context,
                  title: AdminCopy.disable,
                  message: AdminCopy.disableConfirm,
                  danger: true,
                ),
                child: const Text('open'),
              );
            },
          ),
        ),
      ),
    );
    await tester.tap(find.text('open'));
    await tester.pumpAndSettle();
    _expectOnScreen(
      tester,
      find.widgetWithText(FilledButton, AdminCopy.confirm),
    );
    _expectOnScreen(
      tester,
      find.descendant(
        of: find.byType(Dialog),
        matching: find.text(AdminCopy.cancel),
      ),
    );
  });

  testWidgets('login pins an empty username on the field', (tester) async {
    await tester.pumpWidget(
      const ProviderScope(child: _Localized(child: LoginPage())),
    );
    await tester.pumpAndSettle();
    expect(find.text('انتهت الجلسة'), findsNothing);
    await tester.tap(find.text('تسجيل الدخول'));
    await tester.pump();
    final user = tester.widget<TextField>(find.byType(TextField).first);
    final password = tester.widget<TextField>(find.byType(TextField).at(1));
    expect(user.decoration?.errorText, 'هذا الحقل إلزامي');
    expect(password.decoration?.errorText, isNull);
    expect(tester.takeException(), isNull);
  });

  testWidgets('session expiry is visible on the login screen', (tester) async {
    await _setSurface(tester, 360, 700);
    await tester.pumpWidget(
      ProviderScope(
        overrides: [authSessionProvider.overrideWith(_ExpiredSession.new)],
        child: const _Localized(child: LoginPage()),
      ),
    );
    await tester.pumpAndSettle();
    expect(find.text('انتهت الجلسة'), findsOneWidget);
    expect(find.text('الرجاء تسجيل الدخول مجددًا للمتابعة'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('dirty create-owner form warns before leaving', (tester) async {
    final router = GoRouter(
      initialLocation: AppRoutes.adminAccountNew,
      routes: [
        GoRoute(
          path: AppRoutes.adminAccounts,
          builder: (_, _) => const Text('accounts-home'),
        ),
        GoRoute(
          path: AppRoutes.adminAccountNew,
          builder: (_, _) => const Scaffold(body: CreateOwnerPage()),
        ),
      ],
    );
    addTearDown(router.dispose);
    await tester.pumpWidget(
      ProviderScope(
        child: MaterialApp.router(
          locale: const Locale('ar'),
          supportedLocales: kSupportedLocales,
          localizationsDelegates: _delegates,
          routerConfig: router,
        ),
      ),
    );
    await tester.pumpAndSettle();
    await tester.enterText(find.byType(TextField).first, 'ahmad');
    await tester.pump();
    await tester.tap(find.text('← ${AdminCopy.back}'));
    await tester.pumpAndSettle();
    expect(find.text(AdminCopy.discardTitle), findsOneWidget);
    await tester.tap(
      find.descendant(
        of: find.byType(Dialog),
        matching: find.text(AdminCopy.cancel),
      ),
    );
    await tester.pumpAndSettle();
    expect(find.text('accounts-home'), findsNothing);
    expect(find.text(AdminCopy.userName), findsOneWidget);
    await tester.tap(find.text('← ${AdminCopy.back}'));
    await tester.pumpAndSettle();
    await tester.tap(find.widgetWithText(FilledButton, AdminCopy.confirm));
    await tester.pumpAndSettle();
    expect(find.text('accounts-home'), findsOneWidget);
  });

  testWidgets('history tab keeps the route query in sync', (tester) async {
    await _setSurface(tester, 390, 800);
    final router = GoRouter(
      initialLocation: '/owner/devices/dev-1/history',
      routes: [
        GoRoute(
          path: AppRoutes.ownerDeviceHistory,
          builder: (_, state) => DeviceHistoryPage(
            deviceId: state.pathParameters['deviceId'] ?? '',
            initialTab: state.uri.queryParameters['tab'] ?? 'events',
          ),
        ),
      ],
    );
    addTearDown(router.dispose);
    await tester.pumpWidget(
      ProviderScope(
        child: MaterialApp.router(
          locale: const Locale('ar'),
          supportedLocales: kSupportedLocales,
          localizationsDelegates: _delegates,
          routerConfig: router,
        ),
      ),
    );
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
    await tester.tap(find.widgetWithText(Tab, 'الأوامر'));
    await tester.pumpAndSettle();
    expect(router.state.uri.queryParameters['tab'], 'commands');
    expect(tester.takeException(), isNull);
  });

  testWidgets('unknown route renders the arabic not-found page', (
    tester,
  ) async {
    SharedPreferences.setMockInitialValues({});
    final prefs = await SharedPreferences.getInstance();
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          sharedPreferencesProvider.overrideWithValue(prefs),
          appCoreConfigProvider.overrideWithValue(
            const AppCoreConfig(baseUrl: 'http://localhost'),
          ),
          authSessionProvider.overrideWith(_AdminSession.new),
          ownerAccountsViewModelProvider.overrideWith(_AccountsVm.new),
          adminDevicesViewModelProvider.overrideWith(_DevicesVm.new),
        ],
        child: const HisnWebApp(),
      ),
    );
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 50));
    await tester.pumpAndSettle();
    final context = tester.element(find.byType(HisnWebApp));
    ProviderScope.containerOf(
      context,
    ).read(appRouterProvider).go('/missing-web-page');
    await tester.pumpAndSettle();
    expect(find.text('الصفحة غير موجودة'), findsOneWidget);
    expect(find.text('الرئيسية'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}

const _delegates = [
  AppLocalizations.delegate,
  GlobalMaterialLocalizations.delegate,
  GlobalWidgetsLocalizations.delegate,
  GlobalCupertinoLocalizations.delegate,
];

class _Localized extends StatelessWidget {
  const _Localized({required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      locale: const Locale('ar'),
      supportedLocales: kSupportedLocales,
      localizationsDelegates: _delegates,
      home: child,
    );
  }
}

Future<void> _setSurface(
  WidgetTester tester,
  double width,
  double height,
) async {
  tester.view.physicalSize = Size(width, height);
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);
}

Future<void> _pumpDashboard(
  WidgetTester tester, {
  required double width,
  required double height,
  double textScale = 1,
}) async {
  await _setSurface(tester, width, height);
  final router = GoRouter(
    initialLocation: AppRoutes.adminHome,
    routes: [
      ShellRoute(
        builder: (_, _, child) => AdminShell(child: child),
        routes: [
          GoRoute(
            path: AppRoutes.adminHome,
            builder: (_, _) => const AdminDashboardPage(),
          ),
        ],
      ),
    ],
  );
  addTearDown(router.dispose);
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        ownerAccountsViewModelProvider.overrideWith(_AccountsVm.new),
        adminDevicesViewModelProvider.overrideWith(_DevicesVm.new),
      ],
      child: MaterialApp.router(
        locale: const Locale('ar'),
        supportedLocales: kSupportedLocales,
        localizationsDelegates: _delegates,
        builder: (context, child) {
          return MediaQuery(
            data: MediaQuery.of(
              context,
            ).copyWith(textScaler: TextScaler.linear(textScale)),
            child: child ?? const SizedBox.shrink(),
          );
        },
        routerConfig: router,
      ),
    ),
  );
  await tester.pumpAndSettle();
}

void _expectOnScreen(WidgetTester tester, Finder finder) {
  final rect = tester.getRect(finder);
  final size = tester.view.physicalSize / tester.view.devicePixelRatio;
  expect(rect.left, greaterThanOrEqualTo(-0.5));
  expect(rect.top, greaterThanOrEqualTo(-0.5));
  expect(rect.right, lessThanOrEqualTo(size.width + 0.5));
  expect(rect.bottom, lessThanOrEqualTo(size.height + 0.5));
}

class _ExpiredSession extends AuthSessionController {
  @override
  AuthSessionState build() {
    return const AuthSessionState(status: AuthStatus.sessionExpired);
  }
}

class _AdminSession extends AuthSessionController {
  @override
  AuthSessionState build() {
    return const AuthSessionState(
      status: AuthStatus.authenticated,
      user: AuthUserEntity(
        id: 'admin-1',
        userName: 'admin',
        roles: [AppRoleCodes.admin],
      ),
    );
  }

  @override
  Future<void> restoreSession() async {}
}

class _AccountsVm extends OwnerAccountsViewModel {
  @override
  OwnerAccountsState build() {
    return OwnerAccountsState(accounts: [_owner]);
  }
}

class _DevicesVm extends AdminDevicesViewModel {
  @override
  AdminDevicesState build() {
    return AdminDevicesState(devices: [_device]);
  }
}

final _owner = OwnerAccountEntity(
  id: 'owner-1',
  userName: 'ahmad',
  fullName: 'أحمد',
  email: 'ahmad@example.test',
  isActive: true,
  createdAt: DateTime.utc(2026, 11, 1),
);

final _device = AdminDeviceEntity(
  deviceId: 'device-1',
  hardwareUid: 'HW-1',
  displayName: 'المنزل',
  lifecycleStatus: 'REGISTERED',
  currentOwnershipVersion: 1,
  confirmedOwnershipVersion: 0,
  awaitingOwnershipAck: false,
  ownershipStatus: OwnershipStatus.pending,
  ownerFullName: 'أحمد',
  indicators: const AdminDeviceIndicatorsEntity(
    connection: ConnectionStatus.offline,
    arm: ArmStatus.unknown,
    siren: SirenStatus.unknown,
    health: DiagnosticStatus.unknown,
    clockTrusted: false,
  ),
);
