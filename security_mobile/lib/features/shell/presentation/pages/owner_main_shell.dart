import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/router/app_routes.dart';

/// Owner bottom navigation shell aligned with `design/index.html` mobileMenu.
class OwnerMainShell extends StatelessWidget {
  const OwnerMainShell({super.key, required this.navigationShell});

  final StatefulNavigationShell navigationShell;

  @override
  Widget build(BuildContext context) {
    final tr = context.tr;
    return Scaffold(
      body: navigationShell,
      bottomNavigationBar: NavigationBar(
        selectedIndex: navigationShell.currentIndex,
        onDestinationSelected: (index) {
          navigationShell.goBranch(
            index,
            initialLocation: index == navigationShell.currentIndex,
          );
        },
        destinations: [
          NavigationDestination(
            icon: const Icon(Icons.dashboard_outlined),
            selectedIcon: const Icon(Icons.dashboard),
            label: tr.navDashboard,
          ),
          NavigationDestination(
            icon: const Icon(Icons.devices_other_outlined),
            selectedIcon: const Icon(Icons.devices_other),
            label: tr.navDevices,
          ),
          NavigationDestination(
            icon: const Icon(Icons.notifications_outlined),
            selectedIcon: const Icon(Icons.notifications),
            label: tr.navNotifications,
          ),
          NavigationDestination(
            icon: const Icon(Icons.person_outline),
            selectedIcon: const Icon(Icons.person),
            label: tr.navProfile,
          ),
        ],
      ),
    );
  }
}

/// Maps shell branch index ↔ route helpers for tests.
abstract final class OwnerShellTabs {
  static const dashboard = 0;
  static const devices = 1;
  static const notifications = 2;
  static const profile = 3;

  static String locationForIndex(int index) => switch (index) {
        devices => AppRoutes.devices,
        notifications => AppRoutes.notifications,
        profile => AppRoutes.profile,
        _ => AppRoutes.dashboard,
      };
}
