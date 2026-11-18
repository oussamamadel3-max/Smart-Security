import 'package:admin_web/core/router/app_routes.dart';
import 'package:admin_web/features/admin/domain/entities/admin_device_entity.dart';
import 'package:admin_web/features/admin/domain/entities/admin_mutation_entities.dart';
import 'package:admin_web/features/admin/domain/entities/admin_device_indicators_entity.dart';
import 'package:admin_web/features/admin/domain/entities/owner_account_entity.dart';
import 'package:admin_web/features/admin/presentation/admin_copy.dart';
import 'package:admin_web/features/admin/presentation/pages/admin_dashboard_page.dart';
import 'package:admin_web/features/admin/presentation/pages/admin_device_detail_page.dart';
import 'package:admin_web/features/admin/presentation/pages/create_owner_page.dart';
import 'package:admin_web/features/admin/presentation/pages/ownership_pages.dart';
import 'package:admin_web/features/admin/presentation/viewmodels/admin_devices_view_model.dart';
import 'package:admin_web/features/admin/presentation/viewmodels/owner_accounts_view_model.dart';
import 'package:admin_web/features/admin/presentation/viewmodels/ownership_form_view_models.dart';
import 'package:admin_web/features/admin/presentation/widgets/admin_shell.dart';
import 'package:admin_web/features/admin/presentation/widgets/admin_widgets.dart';
import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  GoogleFonts.config.allowRuntimeFetching = false;

  test('admin crumb follows the real admin routes', () {
    expect(adminCrumbTitle(AppRoutes.adminHome), AdminCopy.dashboard);
    expect(adminCrumbTitle(AppRoutes.adminAccountNew), AdminCopy.addAccount);
    expect(adminCrumbTitle('/admin/devices/dev-1/link'), AdminCopy.link);
    expect(adminCrumbTitle('/owner'), AdminCopy.dashboard);
  });

  testWidgets('selected admin nav uses navy2 rather than primary', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(1440, 900);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    final router = GoRouter(
      initialLocation: AppRoutes.adminHome,
      routes: [
        GoRoute(
          path: AppRoutes.adminHome,
          builder: (_, _) => const AdminShell(child: SizedBox.shrink()),
        ),
      ],
    );
    addTearDown(router.dispose);
    await tester.pumpWidget(
      ProviderScope(
        child: MaterialApp.router(
          locale: const Locale('ar'),
          supportedLocales: kSupportedLocales,
          localizationsDelegates: const [
            AppLocalizations.delegate,
            GlobalMaterialLocalizations.delegate,
            GlobalWidgetsLocalizations.delegate,
            GlobalCupertinoLocalizations.delegate,
          ],
          routerConfig: router,
        ),
      ),
    );
    await tester.pumpAndSettle();

    final colors = tester
        .widgetList<Material>(find.byType(Material))
        .map((material) => material.color)
        .toList();
    expect(colors, contains(DesignTokens.navy2));
    expect(colors, isNot(contains(DesignTokens.primary)));
    expect(find.text(AdminCopy.noOwnerCommands), findsOneWidget);
    expect(find.text('حِصن'), findsOneWidget);
  });

  testWidgets('admin table header uses the design surface', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: AdminTable(
            headers: ['الاسم', 'الحالة'],
            rows: [
              [Text('أحمد'), Text('نشط')],
            ],
          ),
        ),
      ),
    );
    final table = tester.widget<Table>(find.byType(Table));
    final header = table.children.first.decoration! as BoxDecoration;
    expect(header.color, AppColors.lightSurfaceVariant);
  });

  testWidgets('create owner pins validation on the userName field', (
    tester,
  ) async {
    await tester.pumpWidget(
      const ProviderScope(
        child: MaterialApp(home: Scaffold(body: CreateOwnerPage())),
      ),
    );
    await tester.tap(find.text(AdminCopy.save));
    await tester.pump();

    final field = tester.widget<TextField>(find.byType(TextField).first);
    expect(field.decoration?.errorText, AdminCopy.fieldMessage('userName'));
    expect(find.text('تفعيل الحماية'), findsNothing);
  });

  testWidgets('confirm dialog uses the design width and danger fill', (
    tester,
  ) async {
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

    expect(
      find.byWidgetPredicate(
        (widget) =>
            widget is ConstrainedBox && widget.constraints.maxWidth == 490,
      ),
      findsOneWidget,
    );
    final button = tester.widget<FilledButton>(
      find.widgetWithText(FilledButton, AdminCopy.confirm),
    );
    expect(
      button.style?.backgroundColor?.resolve(const <WidgetState>{}),
      DesignTokens.dangerSoftBg,
    );
    expect(find.text(AdminCopy.serverConfirms), findsOneWidget);
  });

  testWidgets('dashboard keeps admin scope and hides owner commands', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(1440, 900);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          ownerAccountsViewModelProvider.overrideWith(_AccountsVm.new),
          adminDevicesViewModelProvider.overrideWith(_DevicesVm.new),
        ],
        child: _localized(const AdminDashboardPage()),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text(AdminCopy.eyebrow), findsOneWidget);
    expect(find.text(AdminCopy.noOwnerCommands), findsOneWidget);
    expect(find.text('أحمد'), findsWidgets);
    expect(find.text('تفعيل الحماية'), findsNothing);
    expect(find.text('تعطيل الحماية'), findsNothing);
    expect(find.text('كتم الصفارة'), findsNothing);
  });

  testWidgets('link page shows the ownership path and no arm controls', (
    tester,
  ) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [linkDeviceViewModelProvider.overrideWith(_LinkVm.new)],
        child: _localized(const LinkDevicePage(deviceId: 'device-1')),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text(AdminCopy.stepPending), findsOneWidget);
    expect(find.text(AdminCopy.pendingBanner), findsOneWidget);
    expect(find.text('تفعيل الحماية'), findsNothing);
    expect(find.text('تعطيل الحماية'), findsNothing);
  });

  testWidgets('device detail still has no owner command buttons', (
    tester,
  ) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          adminDeviceDetailViewModelProvider.overrideWith(_DetailVm.new),
        ],
        child: _localized(const AdminDeviceDetailPage(deviceId: 'device-1')),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.textContaining('PENDING'), findsWidgets);
    expect(find.text(AdminCopy.noOwnerCommands), findsOneWidget);
    expect(find.text('تفعيل الحماية'), findsNothing);
    expect(find.text('تعطيل الحماية'), findsNothing);
    expect(find.text('اختبار الصفارة'), findsNothing);
  });
}

Widget _localized(Widget child) {
  return MaterialApp(
    locale: const Locale('ar'),
    supportedLocales: kSupportedLocales,
    localizationsDelegates: const [
      AppLocalizations.delegate,
      GlobalMaterialLocalizations.delegate,
      GlobalWidgetsLocalizations.delegate,
      GlobalCupertinoLocalizations.delegate,
    ],
    home: Scaffold(body: child),
  );
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

class _DetailVm extends AdminDeviceDetailViewModel {
  @override
  AdminDeviceDetailState build(String arg) {
    return AdminDeviceDetailState(device: _device);
  }
}

class _LinkVm extends LinkDeviceViewModel {
  @override
  OwnershipFormState<LinkedDeviceEntity> build(String arg) {
    return OwnershipFormState(owners: [_owner]);
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
