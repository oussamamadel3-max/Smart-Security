import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/auth/presentation/pages/login_page.dart';
import '../../features/bootstrap/presentation/pages/splash_page.dart';
import '../../features/dashboard/presentation/pages/dashboard_page.dart';
import '../../features/devices/presentation/pages/alarm_event_detail_page.dart';
import '../../features/devices/presentation/pages/command_detail_page.dart';
import '../../features/devices/presentation/pages/device_detail_page.dart';
import '../../features/devices/presentation/pages/device_history_page.dart';
import '../../features/devices/presentation/pages/devices_list_page.dart';
import '../../features/notifications/presentation/pages/notifications_page.dart';
import '../../features/profile/presentation/pages/profile_page.dart';
import '../../features/shell/presentation/pages/owner_main_shell.dart';
import '../../features/wifi_provisioning/presentation/pages/wifi_provisioning_page.dart';
import '../session/auth_session_controller.dart';
import '../session/auth_session_state.dart';
import 'app_routes.dart';
import 'auth_guard.dart';

final appRouterProvider = Provider<GoRouter>((ref) {
  final authListenable = ValueNotifier<AuthSessionState>(
    ref.read(authSessionProvider),
  );
  ref.listen<AuthSessionState>(authSessionProvider, (_, next) {
    authListenable.value = next;
  });
  ref.onDispose(authListenable.dispose);

  return GoRouter(
    initialLocation: AppRoutes.splash,
    refreshListenable: authListenable,
    redirect: (context, state) {
      return authGuardRedirect(
        session: ref.read(authSessionProvider),
        matchedLocation: state.matchedLocation,
      );
    },
    routes: [
      GoRoute(
        path: AppRoutes.splash,
        builder: (_, __) => const SplashPage(),
      ),
      GoRoute(
        path: AppRoutes.login,
        builder: (_, __) => const LoginPage(),
      ),
      StatefulShellRoute.indexedStack(
        builder: (context, state, navigationShell) {
          return OwnerMainShell(navigationShell: navigationShell);
        },
        branches: [
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: AppRoutes.dashboard,
                builder: (_, __) => const DashboardPage(),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: AppRoutes.devices,
                builder: (_, __) => const DevicesListPage(),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: AppRoutes.notifications,
                builder: (_, __) => const NotificationsPage(),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: AppRoutes.profile,
                builder: (_, __) => const ProfilePage(),
              ),
            ],
          ),
        ],
      ),
      GoRoute(
        path: AppRoutes.deviceDetail,
        builder: (_, state) => DeviceDetailPage(
          deviceId: state.pathParameters['deviceId']!,
        ),
      ),
      GoRoute(
        path: AppRoutes.deviceHistory,
        builder: (_, state) => DeviceHistoryPage(
          deviceId: state.pathParameters['deviceId']!,
          initialTab: state.uri.queryParameters['tab'] ?? 'events',
        ),
      ),
      GoRoute(
        path: AppRoutes.deviceAlarmEventDetail,
        builder: (_, state) => AlarmEventDetailPage(
          deviceId: state.pathParameters['deviceId']!,
          alarmEventId: state.pathParameters['alarmEventId']!,
        ),
      ),
      GoRoute(
        path: AppRoutes.deviceCommandDetail,
        builder: (_, state) => CommandDetailPage(
          deviceId: state.pathParameters['deviceId']!,
          commandId: state.pathParameters['commandId']!,
        ),
      ),
      GoRoute(
        path: AppRoutes.deviceWifiSetup,
        builder: (_, state) => WifiProvisioningPage(
          deviceId: state.pathParameters['deviceId']!,
        ),
      ),
    ],
  );
});
