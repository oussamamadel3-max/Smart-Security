import 'package:admin_web/core/widgets/owner_visual.dart';
import 'package:admin_web/features/devices/domain/entities/device_indicators_entity.dart';
import 'package:admin_web/features/devices/domain/entities/owner_device_entity.dart';
import 'package:admin_web/features/devices/presentation/widgets/device_card.dart';
import 'package:admin_web/features/devices/presentation/widgets/device_command_bar.dart';
import 'package:admin_web/features/devices/presentation/widgets/device_status_pills.dart';
import 'package:admin_web/features/devices/presentation/widgets/info_notice.dart';
import 'package:admin_web/features/devices/presentation/widgets/state_metric_card.dart';
import 'package:admin_web/features/auth/presentation/pages/login_page.dart';
import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

/// Fixture preview for WEB-C-03. Not registered in the production router.
void main() {
  runApp(const ProviderScope(child: OwnerVisualPreviewApp()));
}

class OwnerVisualPreviewApp extends StatelessWidget {
  const OwnerVisualPreviewApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      debugShowCheckedModeBanner: false,
      locale: const Locale('ar'),
      supportedLocales: kSupportedLocales,
      localizationsDelegates: const [
        AppLocalizations.delegate,
        GlobalMaterialLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
      ],
      theme: AppTheme.light,
      home: const _PreviewHome(),
    );
  }
}

class _PreviewHome extends StatefulWidget {
  const _PreviewHome();

  @override
  State<_PreviewHome> createState() => _PreviewHomeState();
}

class _PreviewHomeState extends State<_PreviewHome> {
  bool _login = false;

  @override
  Widget build(BuildContext context) {
    if (_login) {
      return Stack(
        children: [
          const LoginPage(),
          PositionedDirectional(
            top: 12,
            start: 12,
            child: _switcher(),
          ),
        ],
      );
    }
    final width = MediaQuery.sizeOf(context).width;
    final columns = OwnerLayout.deviceColumns(width);
    return Scaffold(
      backgroundColor: DesignTokens.background,
      appBar: AppBar(
        toolbarHeight: OwnerLayout.topBarHeight,
        centerTitle: false,
        title: Text(
          'معاينة مطابقة المالك — بيانات ثابتة',
          style: Theme.of(context).textTheme.labelMedium,
        ),
        actions: [_switcher()],
      ),
      body: Align(
        alignment: Alignment.topCenter,
        child: ConstrainedBox(
          constraints: const BoxConstraints(
            maxWidth: OwnerLayout.contentMaxWidth,
          ),
          child: ListView(
            padding: OwnerLayout.pagePadding(width),
            children: [
              const OwnerPageHeader(
                title: 'أجهزتي',
                subtitle: 'استعرض الأجهزة المرتبطة بحسابك وحالاتها المؤكدة.',
              ),
              const InfoNotice(
                text:
                    'الأجهزة التي لم يكتمل إقرار ربطها تُعرض باسمها وحالة الربط فقط، دون التحكم أو السجلات.',
              ),
              LayoutBuilder(
                builder: (context, constraints) {
                  const gap = DesignTokens.spaceMd;
                  final itemWidth = columns == 1
                      ? constraints.maxWidth
                      : (constraints.maxWidth - gap) / 2;
                  return Wrap(
                    spacing: gap,
                    runSpacing: gap,
                    children: [
                      for (final device in _devices)
                        SizedBox(
                          width: itemWidth,
                          child: DeviceCard(device: device),
                        ),
                    ],
                  );
                },
              ),
              const SizedBox(height: DesignTokens.spaceXl),
              const OwnerPageHeader(
                title: 'تفاصيل الجهاز',
                subtitle: OwnerVisualCopy.deviceDetailSubtitle,
              ),
              const InfoNotice(
                text:
                    'هذا الجهاز غير متصل. تظهر آخر حالة معروفة وليست الحالة اللحظية.',
                tone: InfoNoticeTone.warning,
              ),
              const StateMetricGrid(
                children: [
                  StateMetricCard(
                    title: OwnerVisualCopy.connectionTitle,
                    value: 'غير متصل',
                    hint: 'آخر نبضة: قبل 38 دقيقة',
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
                    value: OwnerVisualCopy.unknownShort,
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
              const SizedBox(height: DesignTokens.spaceLg),
              DeviceCommandBar(
                enabled: false,
                submitting: false,
                onArm: () {},
                onDisarm: () {},
                onMute: () {},
                onTest: () {},
                onStopTest: () {},
                muteEnabled: false,
                testEnabled: false,
                stopTestEnabled: false,
              ),
              const SizedBox(height: DesignTokens.spaceLg),
              const Wrap(
                spacing: 8,
                runSpacing: 8,
                children: [
                  StatusPill(
                    label: OwnerVisualCopy.executed,
                    tone: StatusPillTone.green,
                  ),
                  StatusPill(
                    label: OwnerVisualCopy.pending,
                    tone: StatusPillTone.amber,
                  ),
                  StatusPill(
                    label: OwnerVisualCopy.timedOut,
                    tone: StatusPillTone.red,
                  ),
                  StatusPill(
                    label: OwnerVisualCopy.eventEnded,
                    tone: StatusPillTone.green,
                  ),
                  StatusPill(label: 'متأخرة', tone: StatusPillTone.amber),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _switcher() {
    return Padding(
      padding: const EdgeInsetsDirectional.only(end: 12),
      child: TextButton(
        onPressed: () => setState(() => _login = !_login),
        child: Text(_login ? 'شاشات المالك' : 'تسجيل الدخول'),
      ),
    );
  }
}

final _devices = [
  OwnerDeviceEntity(
    deviceId: 'SEC-001',
    displayName: 'المنزل الرئيسي',
    ownershipStatus: OwnershipStatus.active,
    ownershipVersion: 3,
    confirmedOwnershipVersion: 3,
    awaitingOwnershipAck: false,
    canControl: true,
    linkStatusMessage: 'المدخل الرئيسي',
    indicators: DeviceIndicatorsEntity(
      connection: ConnectionStatus.online,
      arm: ArmStatus.armed,
      siren: SirenStatus.off,
      health: DiagnosticStatus.unknown,
      lastSeenAt: DateTime.now().subtract(const Duration(seconds: 15)),
    ),
  ),
  OwnerDeviceEntity(
    deviceId: 'SEC-002',
    displayName: 'المستودع',
    ownershipStatus: OwnershipStatus.active,
    ownershipVersion: 2,
    confirmedOwnershipVersion: 2,
    awaitingOwnershipAck: false,
    canControl: false,
    linkStatusMessage: 'المدخل الشرقي',
    indicators: DeviceIndicatorsEntity(
      connection: ConnectionStatus.offline,
      arm: ArmStatus.armed,
      siren: SirenStatus.unknown,
      health: DiagnosticStatus.unknown,
      lastSeenAt: DateTime.now().subtract(const Duration(minutes: 38)),
    ),
  ),
  const OwnerDeviceEntity(
    deviceId: 'SEC-003',
    displayName: 'البوابة',
    ownershipStatus: OwnershipStatus.pending,
    ownershipVersion: 1,
    confirmedOwnershipVersion: 0,
    awaitingOwnershipAck: true,
    canControl: false,
    linkStatusMessage: '',
  ),
];
