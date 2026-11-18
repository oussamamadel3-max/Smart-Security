import 'package:admin_web/core/widgets/owner_visual.dart';
import 'package:admin_web/features/auth/presentation/pages/login_page.dart';
import 'package:admin_web/features/devices/domain/entities/device_indicators_entity.dart';
import 'package:admin_web/features/devices/domain/entities/owner_device_entity.dart';
import 'package:admin_web/features/devices/presentation/widgets/device_card.dart';
import 'package:admin_web/features/devices/presentation/widgets/device_command_bar.dart';
import 'package:admin_web/features/devices/presentation/widgets/device_status_pills.dart';
import 'package:admin_web/features/devices/presentation/widgets/state_metric_card.dart';
import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  GoogleFonts.config.allowRuntimeFetching = false;

  test('owner layout breakpoints follow design/index.html', () {
    expect(OwnerLayout.loginMaxWidth, 475);
    expect(OwnerLayout.deviceColumns(780), 1);
    expect(OwnerLayout.deviceColumns(781), 2);
    expect(OwnerLayout.metricColumns(1100), 2);
    expect(OwnerLayout.metricColumns(1101), 4);
    expect(OwnerLayout.topBarHeight, 78);
  });

  testWidgets('blue and gray pills use design token fills', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: Column(
            children: [
              StatusPill(label: 'blue', tone: StatusPillTone.blue),
              StatusPill(label: 'gray', tone: StatusPillTone.gray),
            ],
          ),
        ),
      ),
    );

    final boxes = tester
        .widgetList<Container>(find.byType(Container))
        .map((box) => (box.decoration as BoxDecoration).color)
        .toList();
    expect(boxes, contains(DesignTokens.infoSoftBg));
    expect(boxes, contains(AppColors.lightSurfaceVariant));
    expect(boxes, isNot(contains(DesignTokens.primarySoft)));
  });

  testWidgets('pending card keeps link status and hides arm controls', (
    tester,
  ) async {
    await tester.pumpWidget(
      _localized(
        DeviceCard(
          device: const OwnerDeviceEntity(
            deviceId: 'pending-1',
            displayName: 'باب جانبي',
            ownershipStatus: OwnershipStatus.pending,
            ownershipVersion: 1,
            confirmedOwnershipVersion: 0,
            awaitingOwnershipAck: true,
            canControl: false,
            linkStatusMessage: '',
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('باب جانبي'), findsOneWidget);
    expect(find.text('الربط قيد الاكتمال'), findsWidgets);
    expect(find.text('الحماية مفعّلة'), findsNothing);
    expect(find.text('تفعيل الحماية'), findsNothing);

    expect(
      find.byWidgetPredicate(
        (widget) =>
            widget is Material &&
            widget.borderRadius ==
                BorderRadius.circular(DesignTokens.radiusLg),
      ),
      findsOneWidget,
    );
  });

  testWidgets('active card shows confirmed arm and connection pills', (
    tester,
  ) async {
    await tester.pumpWidget(
      _localized(
        DeviceCard(
          device: OwnerDeviceEntity(
            deviceId: 'sec-1',
            displayName: 'المنزل الرئيسي',
            ownershipStatus: OwnershipStatus.active,
            ownershipVersion: 2,
            confirmedOwnershipVersion: 2,
            awaitingOwnershipAck: false,
            canControl: true,
            linkStatusMessage: 'المدخل الرئيسي',
            indicators: DeviceIndicatorsEntity(
              connection: ConnectionStatus.online,
              arm: ArmStatus.armed,
              siren: SirenStatus.off,
              health: DiagnosticStatus.unknown,
              lastSeenAt: DateTime.utc(2026, 11, 18, 9),
            ),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('متصل'), findsOneWidget);
    expect(find.text('الحماية مفعّلة'), findsOneWidget);
    expect(find.text('سلامة العتاد غير معروفة'), findsOneWidget);
    expect(find.text('الصفارة متوقفة'), findsOneWidget);
  });

  testWidgets('command bar uses primary arm and soft disarm', (tester) async {
    await tester.pumpWidget(
      _localized(
        DeviceCommandBar(
          enabled: true,
          submitting: false,
          onArm: () {},
          onDisarm: () {},
          onMute: () {},
          onTest: () {},
          onStopTest: () {},
          muteEnabled: false,
          testEnabled: true,
          stopTestEnabled: false,
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('♜  تفعيل الحماية'), findsOneWidget);
    expect(find.text('♙  تعطيل الحماية'), findsOneWidget);
    expect(find.text('♫  اختبار الصفارة'), findsOneWidget);

    final disarm = tester.widget<OutlinedButton>(
      find.widgetWithText(OutlinedButton, '♙  تعطيل الحماية'),
    );
    final background = disarm.style?.backgroundColor?.resolve({});
    expect(background, DesignTokens.primarySoft);
  });

  testWidgets('state metrics keep the four design titles', (tester) async {
    await tester.pumpWidget(
      _localized(
        const StateMetricGrid(
          children: [
            StateMetricCard(
              title: OwnerVisualCopy.connectionTitle,
              value: 'متصل',
              hint: 'آخر نبضة',
              symbol: '◉',
            ),
            StateMetricCard(
              title: OwnerVisualCopy.protectionTitle,
              value: OwnerVisualCopy.armedShort,
              hint: OwnerVisualCopy.lastAckHint,
              symbol: '♜',
            ),
            StateMetricCard(
              title: OwnerVisualCopy.sirenTitle,
              value: OwnerVisualCopy.sirenOffShort,
              hint: OwnerVisualCopy.sirenHint,
              symbol: '◖',
            ),
            StateMetricCard(
              title: OwnerVisualCopy.hardwareTitle,
              value: OwnerVisualCopy.unknownShort,
              hint: OwnerVisualCopy.hardwareHint,
              symbol: '⚑',
            ),
          ],
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('حالة الاتصال'), findsOneWidget);
    expect(find.text('حالة الحماية'), findsOneWidget);
    expect(find.text('حالة الصفارة'), findsOneWidget);
    expect(find.text('سلامة العتاد'), findsOneWidget);
    expect(find.text('مفعّلة'), findsOneWidget);
  });

  testWidgets('login keeps the design width and empty fields', (tester) async {
    await tester.pumpWidget(
      ProviderScope(
        child: MaterialApp(
          locale: const Locale('ar'),
          supportedLocales: kSupportedLocales,
          localizationsDelegates: const [
            AppLocalizations.delegate,
            GlobalMaterialLocalizations.delegate,
            GlobalWidgetsLocalizations.delegate,
            GlobalCupertinoLocalizations.delegate,
          ],
          theme: AppTheme.light,
          home: const LoginPage(),
        ),
      ),
    );
    await tester.pump();

    expect(find.text('مرحباً بعودتك'), findsOneWidget);
    expect(find.text('ahmad@example.test'), findsNothing);
    expect(
      find.byWidgetPredicate(
        (widget) =>
            widget is ConstrainedBox && widget.constraints.maxWidth == 475,
      ),
      findsOneWidget,
    );
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
    theme: AppTheme.light,
    home: Scaffold(body: child),
  );
}
