import 'package:admin_web/core/router/app_routes.dart';
import 'package:admin_web/features/admin/domain/entities/admin_alert_entity.dart';
import 'package:admin_web/features/admin/domain/entities/admin_device_entity.dart';
import 'package:admin_web/features/admin/domain/entities/admin_device_indicators_entity.dart';
import 'package:admin_web/features/admin/domain/entities/admin_mutation_entities.dart';
import 'package:admin_web/features/admin/domain/entities/audit_log_entity.dart';
import 'package:admin_web/features/admin/domain/entities/owner_account_entity.dart';
import 'package:admin_web/features/admin/presentation/pages/admin_alerts_page.dart';
import 'package:admin_web/features/admin/presentation/pages/admin_dashboard_page.dart';
import 'package:admin_web/features/admin/presentation/pages/admin_device_detail_page.dart';
import 'package:admin_web/features/admin/presentation/pages/admin_devices_page.dart';
import 'package:admin_web/features/admin/presentation/pages/audit_logs_page.dart';
import 'package:admin_web/features/admin/presentation/pages/create_owner_page.dart';
import 'package:admin_web/features/admin/presentation/pages/owner_account_detail_page.dart';
import 'package:admin_web/features/admin/presentation/pages/owner_accounts_page.dart';
import 'package:admin_web/features/admin/presentation/pages/ownership_pages.dart';
import 'package:admin_web/features/admin/presentation/pages/register_device_page.dart';
import 'package:admin_web/features/admin/presentation/viewmodels/admin_alerts_view_model.dart';
import 'package:admin_web/features/admin/presentation/viewmodels/admin_devices_view_model.dart';
import 'package:admin_web/features/admin/presentation/viewmodels/audit_logs_view_model.dart';
import 'package:admin_web/features/admin/presentation/viewmodels/owner_account_form_view_models.dart';
import 'package:admin_web/features/admin/presentation/viewmodels/owner_accounts_view_model.dart';
import 'package:admin_web/features/admin/presentation/viewmodels/ownership_form_view_models.dart';
import 'package:admin_web/features/admin/presentation/widgets/admin_shell.dart';
import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

/// Fixture preview for WEB-C-04. Not registered in the production router.
void main() {
  runApp(
    ProviderScope(
      overrides: [
        ownerAccountsViewModelProvider.overrideWith(_AccountsVm.new),
        adminDevicesViewModelProvider.overrideWith(_DevicesVm.new),
        auditLogsViewModelProvider.overrideWith(_AuditVm.new),
        adminAlertsViewModelProvider.overrideWith(_AlertsVm.new),
        ownerAccountDetailViewModelProvider.overrideWith(_AccountDetailVm.new),
        adminDeviceDetailViewModelProvider.overrideWith(_DeviceDetailVm.new),
        linkDeviceViewModelProvider.overrideWith(_LinkVm.new),
        transferDeviceViewModelProvider.overrideWith(_TransferVm.new),
        adminAlertDetailViewModelProvider.overrideWith(_AlertDetailVm.new),
      ],
      child: const AdminVisualPreviewApp(),
    ),
  );
}

class AdminVisualPreviewApp extends StatelessWidget {
  const AdminVisualPreviewApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp.router(
      debugShowCheckedModeBanner: false,
      title: 'معاينة الإدارة',
      locale: const Locale('ar'),
      supportedLocales: kSupportedLocales,
      localizationsDelegates: const [
        AppLocalizations.delegate,
        GlobalMaterialLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
      ],
      theme: AppTheme.light,
      routerConfig: _router,
    );
  }
}

final _router = GoRouter(
  initialLocation: AppRoutes.adminHome,
  routes: [
    ShellRoute(
      builder: (_, _, child) => AdminShell(child: child),
      routes: [
        GoRoute(
          path: AppRoutes.adminHome,
          builder: (_, _) => const AdminDashboardPage(),
          routes: [
            GoRoute(
              path: 'accounts',
              builder: (_, _) => const OwnerAccountsPage(),
              routes: [
                GoRoute(
                  path: 'new',
                  builder: (_, _) => const CreateOwnerPage(),
                ),
                GoRoute(
                  path: ':ownerId',
                  builder: (_, state) => OwnerAccountDetailPage(
                    ownerId: state.pathParameters['ownerId'] ?? '',
                  ),
                ),
              ],
            ),
            GoRoute(
              path: 'devices',
              builder: (_, _) => const AdminDevicesPage(),
              routes: [
                GoRoute(
                  path: 'register',
                  builder: (_, _) => const RegisterDevicePage(),
                ),
                GoRoute(
                  path: ':deviceId',
                  builder: (_, state) => AdminDeviceDetailPage(
                    deviceId: state.pathParameters['deviceId'] ?? '',
                  ),
                  routes: [
                    GoRoute(
                      path: 'link',
                      builder: (_, state) => LinkDevicePage(
                        deviceId: state.pathParameters['deviceId'] ?? '',
                      ),
                    ),
                    GoRoute(
                      path: 'transfer',
                      builder: (_, state) => TransferDevicePage(
                        deviceId: state.pathParameters['deviceId'] ?? '',
                      ),
                    ),
                  ],
                ),
              ],
            ),
            GoRoute(
              path: 'audit-logs',
              builder: (_, _) => const AuditLogsPage(),
              routes: [
                GoRoute(
                  path: ':logId',
                  builder: (_, state) => AuditLogDetailPage(
                    logId: state.pathParameters['logId'] ?? '',
                  ),
                ),
              ],
            ),
            GoRoute(
              path: 'alerts',
              builder: (_, _) => const AdminAlertsPage(),
              routes: [
                GoRoute(
                  path: 'new',
                  builder: (_, _) => const CreateAdminAlertPage(),
                ),
                GoRoute(
                  path: ':alertId',
                  builder: (_, state) => AdminAlertDetailPage(
                    alertId: state.pathParameters['alertId'] ?? '',
                  ),
                ),
              ],
            ),
          ],
        ),
      ],
    ),
  ],
);

class _AccountsVm extends OwnerAccountsViewModel {
  @override
  OwnerAccountsState build() => OwnerAccountsState(accounts: _accounts);
}

class _DevicesVm extends AdminDevicesViewModel {
  @override
  AdminDevicesState build() => const AdminDevicesState(devices: _devices);
}

class _AuditVm extends AuditLogsViewModel {
  @override
  AuditLogsState build() {
    return AuditLogsState(items: _logs, totalCount: _logs.length);
  }
}

class _AlertsVm extends AdminAlertsViewModel {
  @override
  AdminAlertsState build() {
    return AdminAlertsState(items: _alerts, totalCount: _alerts.length);
  }
}

class _AccountDetailVm extends OwnerAccountDetailViewModel {
  @override
  OwnerAccountDetailState build(String arg) {
    return OwnerAccountDetailState(
      account: _accounts.firstWhere((account) => account.id == arg),
    );
  }
}

class _DeviceDetailVm extends AdminDeviceDetailViewModel {
  @override
  AdminDeviceDetailState build(String arg) {
    return AdminDeviceDetailState(
      device: _devices.firstWhere((device) => device.deviceId == arg),
    );
  }
}

class _LinkVm extends LinkDeviceViewModel {
  @override
  OwnershipFormState<LinkedDeviceEntity> build(String arg) {
    return OwnershipFormState(
      owners: _accounts.where((account) => account.isActive).toList(),
    );
  }
}

class _TransferVm extends TransferDeviceViewModel {
  @override
  OwnershipFormState<TransferredDeviceEntity> build(String arg) {
    return OwnershipFormState(
      owners: _accounts.where((account) => account.isActive).toList(),
      excludedOwnerId: 'owner-1',
    );
  }
}

class _AlertDetailVm extends AdminAlertDetailViewModel {
  @override
  AdminAlertDetailState build(String arg) {
    return AdminAlertDetailState(
      alert: _alerts.firstWhere((alert) => alert.adminAlertId == arg),
    );
  }
}

final _accounts = [
  OwnerAccountEntity(
    id: 'owner-1',
    userName: 'ahmad',
    fullName: 'أحمد محمود',
    email: 'ahmad@example.test',
    isActive: true,
    createdAt: DateTime.utc(2026, 10, 2),
  ),
  OwnerAccountEntity(
    id: 'owner-2',
    userName: 'layla',
    fullName: 'ليلى خالد',
    email: 'layla@example.test',
    isActive: true,
    createdAt: DateTime.utc(2026, 10, 4),
  ),
  OwnerAccountEntity(
    id: 'owner-3',
    userName: 'samer',
    fullName: 'محمد سامر',
    email: 'samer@example.test',
    isActive: false,
    createdAt: DateTime.utc(2026, 9, 12),
  ),
];

const _devices = [
  AdminDeviceEntity(
    deviceId: 'device-1',
    hardwareUid: 'SEC-001',
    displayName: 'المنزل الرئيسي',
    lifecycleStatus: 'REGISTERED',
    currentOwnershipVersion: 2,
    confirmedOwnershipVersion: 2,
    awaitingOwnershipAck: false,
    ownershipStatus: OwnershipStatus.active,
    ownerFullName: 'أحمد محمود',
    indicators: AdminDeviceIndicatorsEntity(
      connection: ConnectionStatus.online,
      arm: ArmStatus.armed,
      siren: SirenStatus.off,
      health: DiagnosticStatus.unknown,
      clockTrusted: true,
      lastSeenAt: null,
    ),
  ),
  AdminDeviceEntity(
    deviceId: 'device-2',
    hardwareUid: 'SEC-002',
    displayName: 'المستودع',
    lifecycleStatus: 'REGISTERED',
    currentOwnershipVersion: 1,
    confirmedOwnershipVersion: 0,
    awaitingOwnershipAck: false,
    ownershipStatus: OwnershipStatus.pending,
    ownerFullName: 'أحمد محمود',
    indicators: AdminDeviceIndicatorsEntity(
      connection: ConnectionStatus.offline,
      arm: ArmStatus.unknown,
      siren: SirenStatus.unknown,
      health: DiagnosticStatus.unknown,
      clockTrusted: false,
    ),
  ),
];

final _logs = [
  AuditLogEntity(
    id: 'AUD-112',
    action: 'OWNERSHIP_TRANSFERRED',
    targetType: 'Device',
    result: 'SUCCESS',
    occurredAt: DateTime.utc(2026, 11, 18, 8, 20),
    targetId: 'device-2',
    deviceId: 'device-2',
    actorUserId: 'admin-1',
  ),
  AuditLogEntity(
    id: 'AUD-111',
    action: 'DEVICE_REGISTERED',
    targetType: 'Device',
    result: 'SUCCESS',
    occurredAt: DateTime.utc(2026, 11, 17, 16, 30),
    deviceId: 'device-1',
    actorUserId: 'admin-1',
  ),
];

final _alerts = [
  AdminAlertEntity(
    adminAlertId: 'alert-1',
    senderUserId: 'admin-1',
    recipientUserId: 'owner-1',
    title: 'جهاز غير متصل — المستودع',
    body: 'فقد الاتصال وفق مهلة النبضة المعتمدة.',
    createdAt: DateTime.utc(2026, 11, 18, 9, 12),
    deliveryAttemptStatus: 'PENDING',
    deliveryAttempted: false,
  ),
];
