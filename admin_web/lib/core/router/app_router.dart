import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/admin/presentation/pages/admin_alerts_page.dart';
import '../../features/admin/presentation/pages/admin_dashboard_page.dart';
import '../../features/admin/presentation/pages/audit_logs_page.dart';
import '../../features/admin/presentation/pages/admin_device_detail_page.dart';
import '../../features/admin/presentation/pages/admin_devices_page.dart';
import '../../features/admin/presentation/pages/create_owner_page.dart';
import '../../features/admin/presentation/pages/owner_account_detail_page.dart';
import '../../features/admin/presentation/pages/owner_accounts_page.dart';
import '../../features/admin/presentation/pages/ownership_pages.dart';
import '../../features/admin/presentation/pages/register_device_page.dart';
import '../../features/admin/presentation/widgets/admin_shell.dart';
import '../../features/auth/presentation/pages/login_page.dart';
import '../../features/bootstrap/presentation/pages/not_found_page.dart';
import '../../features/bootstrap/presentation/pages/splash_page.dart';
import '../../features/devices/presentation/pages/alarm_event_detail_page.dart';
import '../../features/devices/presentation/pages/command_detail_page.dart';
import '../../features/devices/presentation/pages/device_detail_page.dart';
import '../../features/devices/presentation/pages/device_history_page.dart';
import '../../features/devices/presentation/pages/devices_list_page.dart';
import '../session/auth_session_controller.dart';
import '../session/auth_session_state.dart';
import 'app_route_observer.dart';
import 'app_routes.dart';
import 'auth_guard.dart';

final appRouterProvider = Provider<GoRouter>((ref) {
  final listenable = ValueNotifier<AuthSessionState>(
    ref.read(authSessionProvider),
  );
  ref.listen<AuthSessionState>(authSessionProvider, (_, next) {
    listenable.value = next;
  });
  ref.onDispose(listenable.dispose);

  return GoRouter(
    initialLocation: AppRoutes.splash,
    refreshListenable: listenable,
    observers: [appRouteObserver],
    redirect: (context, state) {
      return authGuardRedirect(
        session: ref.read(authSessionProvider),
        matchedLocation: state.matchedLocation,
      );
    },
    errorBuilder: (_, _) => const NotFoundPage(),
    routes: [
      GoRoute(path: AppRoutes.splash, builder: (_, __) => const SplashPage()),
      GoRoute(path: AppRoutes.login, builder: (_, __) => const LoginPage()),
      GoRoute(
        path: AppRoutes.ownerHome,
        builder: (_, __) => const DevicesListPage(),
      ),
      GoRoute(
        path: AppRoutes.ownerDeviceAlarmEvent,
        builder: (_, state) => AlarmEventDetailPage(
          deviceId: state.pathParameters['deviceId'] ?? '',
          alarmEventId: state.pathParameters['alarmEventId'] ?? '',
        ),
      ),
      GoRoute(
        path: AppRoutes.ownerDeviceCommand,
        builder: (_, state) => CommandDetailPage(
          deviceId: state.pathParameters['deviceId'] ?? '',
          commandId: state.pathParameters['commandId'] ?? '',
        ),
      ),
      GoRoute(
        path: AppRoutes.ownerDeviceHistory,
        builder: (_, state) => DeviceHistoryPage(
          deviceId: state.pathParameters['deviceId'] ?? '',
          initialTab: state.uri.queryParameters['tab'] ?? 'events',
        ),
      ),
      GoRoute(
        path: AppRoutes.ownerDeviceDetail,
        builder: (_, state) =>
            DeviceDetailPage(deviceId: state.pathParameters['deviceId'] ?? ''),
      ),
      ShellRoute(
        builder: (_, __, child) => AdminShell(child: child),
        routes: [
          GoRoute(
            path: AppRoutes.adminHome,
            builder: (_, __) => const AdminDashboardPage(),
            routes: [
              GoRoute(
                path: 'accounts',
                builder: (_, __) => const OwnerAccountsPage(),
                routes: [
                  GoRoute(
                    path: 'new',
                    builder: (_, __) => const CreateOwnerPage(),
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
                builder: (_, __) => const AdminDevicesPage(),
                routes: [
                  GoRoute(
                    path: 'register',
                    builder: (_, __) => const RegisterDevicePage(),
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
                builder: (_, __) => const AuditLogsPage(),
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
                builder: (_, __) => const AdminAlertsPage(),
                routes: [
                  GoRoute(
                    path: 'new',
                    builder: (_, __) => const CreateAdminAlertPage(),
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
});
