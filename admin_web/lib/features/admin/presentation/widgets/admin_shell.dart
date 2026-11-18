import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/router/app_routes.dart';
import '../../../../core/session/auth_session_controller.dart';
import '../../../../core/widgets/owner_visual.dart';
import '../admin_copy.dart';

class AdminShell extends ConsumerWidget {
  const AdminShell({super.key, required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final wide = MediaQuery.sizeOf(context).width >= 960;
    return Scaffold(
      backgroundColor: DesignTokens.background,
      drawer: wide ? null : const Drawer(child: AdminSidebar(inDrawer: true)),
      body: Row(
        children: [
          if (wide) const SizedBox(width: 256, child: AdminSidebar()),
          Expanded(
            child: Column(
              children: [
                const AdminTopBar(),
                Expanded(child: child),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class AdminTopBar extends ConsumerWidget {
  const AdminTopBar({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final width = MediaQuery.sizeOf(context).width;
    final wide = width >= 960;
    final roomy = width > 780;
    final user = ref.watch(authSessionProvider.select((s) => s.user));
    final name = user?.fullName ?? user?.userName ?? '';
    final trimmed = name.trim();
    final initial = trimmed.isEmpty ? 'م' : trimmed.substring(0, 1);
    final path = GoRouterState.of(context).uri.path;
    return Material(
      color: DesignTokens.card,
      child: Container(
        constraints: BoxConstraints(
          minHeight: roomy ? OwnerLayout.topBarHeight : 65,
        ),
        padding: EdgeInsets.symmetric(
          horizontal: roomy ? DesignTokens.spaceXxl : 15,
          vertical: roomy ? 0 : 10,
        ),
        decoration: const BoxDecoration(
          border: Border(bottom: BorderSide(color: DesignTokens.border)),
        ),
        child: Row(
          children: [
            if (!wide) ...[
              _TopIconButton(
                tooltip: 'القائمة',
                onPressed: () => Scaffold.of(context).openDrawer(),
                icon: Icons.menu,
              ),
              const SizedBox(width: 12),
            ],
            Expanded(child: _Crumb(title: adminCrumbTitle(path))),
            _TopIconButton(
              tooltip: context.tr.logout,
              onPressed: () => ref.read(authSessionProvider.notifier).logout(),
              icon: Icons.logout,
            ),
            const SizedBox(width: 12),
            _Avatar(initial: initial),
          ],
        ),
      ),
    );
  }
}

class _Crumb extends StatelessWidget {
  const _Crumb({required this.title});

  final String title;

  @override
  Widget build(BuildContext context) {
    final base = Theme.of(context).textTheme.labelMedium?.copyWith(
      color: DesignTokens.muted,
      fontWeight: FontWeight.w500,
    );
    return Text.rich(
      TextSpan(
        style: base,
        children: [
          TextSpan(text: '${AdminCopy.homeCrumb} / '),
          TextSpan(
            text: title,
            style: base?.copyWith(
              color: DesignTokens.ink,
              fontWeight: FontWeight.w700,
            ),
          ),
        ],
      ),
      maxLines: 2,
      overflow: TextOverflow.ellipsis,
    );
  }
}

class _TopIconButton extends StatelessWidget {
  const _TopIconButton({
    required this.tooltip,
    required this.onPressed,
    required this.icon,
  });

  final String tooltip;
  final VoidCallback onPressed;
  final IconData icon;

  @override
  Widget build(BuildContext context) {
    return Tooltip(
      message: tooltip,
      child: InkWell(
        onTap: onPressed,
        borderRadius: BorderRadius.circular(10),
        child: Container(
          width: 36,
          height: 36,
          alignment: Alignment.center,
          decoration: BoxDecoration(
            color: DesignTokens.card,
            borderRadius: BorderRadius.circular(10),
            border: Border.all(color: DesignTokens.border),
          ),
          child: Icon(icon, size: 18, color: DesignTokens.ink),
        ),
      ),
    );
  }
}

class _Avatar extends StatelessWidget {
  const _Avatar({required this.initial});

  final String initial;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 37,
      height: 37,
      alignment: Alignment.center,
      decoration: BoxDecoration(
        color: DesignTokens.infoSoftBg,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Text(
        initial,
        style: const TextStyle(
          color: DesignTokens.primaryTextSoft,
          fontWeight: FontWeight.w800,
        ),
      ),
    );
  }
}

String adminCrumbTitle(String path) {
  if (path == AppRoutes.adminAccountNew) return AdminCopy.addAccount;
  if (path.startsWith('${AppRoutes.adminAccounts}/')) {
    return AdminCopy.accountDetail;
  }
  if (path == AppRoutes.adminAccounts) return AdminCopy.accounts;
  if (path == AppRoutes.adminDeviceRegister) return AdminCopy.registerDevice;
  if (path.endsWith('/link')) return AdminCopy.link;
  if (path.endsWith('/transfer')) return AdminCopy.transfer;
  if (path.startsWith('${AppRoutes.adminDevices}/')) {
    return AdminCopy.deviceDetail;
  }
  if (path == AppRoutes.adminDevices) return AdminCopy.devices;
  if (path.startsWith('${AppRoutes.adminAuditLogs}/')) {
    return AdminCopy.auditDetail;
  }
  if (path == AppRoutes.adminAuditLogs) return AdminCopy.auditLogs;
  if (path == AppRoutes.adminAlertNew) return AdminCopy.createAlert;
  if (path.startsWith('${AppRoutes.adminAlerts}/')) {
    return AdminCopy.alertDetail;
  }
  if (path == AppRoutes.adminAlerts) return AdminCopy.alerts;
  return AdminCopy.dashboard;
}

class AdminSidebar extends StatelessWidget {
  const AdminSidebar({super.key, this.inDrawer = false});

  final bool inDrawer;

  @override
  Widget build(BuildContext context) {
    final path = GoRouterState.of(context).uri.path;
    return ColoredBox(
      color: DesignTokens.navy,
      child: SafeArea(
        child: ListView(
          padding: const EdgeInsets.fromLTRB(15, 25, 15, 25),
          children: [
            const _Brand(),
            const SizedBox(height: 12),
            Padding(
              padding: const EdgeInsets.fromLTRB(14, 12, 14, 7),
              child: Text(
                AdminCopy.shellLabel,
                style: Theme.of(context).textTheme.labelSmall?.copyWith(
                  color: const Color(0xFF7591AE),
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
            _NavButton(
              label: AdminCopy.dashboard,
              icon: Icons.grid_view,
              selected: path == AppRoutes.adminHome,
              onTap: () => _go(context, AppRoutes.adminHome),
            ),
            _NavButton(
              label: AdminCopy.accounts,
              icon: Icons.people_outline,
              selected:
                  path.startsWith(AppRoutes.adminAccounts) &&
                  path != AppRoutes.adminAccountNew,
              onTap: () => _go(context, AppRoutes.adminAccounts),
            ),
            _NavButton(
              label: AdminCopy.addAccount,
              icon: Icons.person_add_alt_1_outlined,
              selected: path == AppRoutes.adminAccountNew,
              onTap: () => _go(context, AppRoutes.adminAccountNew),
            ),
            _NavButton(
              label: AdminCopy.devices,
              icon: Icons.devices_outlined,
              selected:
                  path.startsWith(AppRoutes.adminDevices) &&
                  path != AppRoutes.adminDeviceRegister,
              onTap: () => _go(context, AppRoutes.adminDevices),
            ),
            _NavButton(
              label: AdminCopy.registerDevice,
              icon: Icons.add_box_outlined,
              selected: path == AppRoutes.adminDeviceRegister,
              onTap: () => _go(context, AppRoutes.adminDeviceRegister),
            ),
            _NavButton(
              label: AdminCopy.auditLogs,
              icon: Icons.receipt_long_outlined,
              selected: path.startsWith(AppRoutes.adminAuditLogs),
              onTap: () => _go(context, AppRoutes.adminAuditLogs),
            ),
            _NavButton(
              label: AdminCopy.alerts,
              icon: Icons.campaign_outlined,
              selected: path.startsWith(AppRoutes.adminAlerts),
              onTap: () => _go(context, AppRoutes.adminAlerts),
            ),
            const _SidebarNote(),
          ],
        ),
      ),
    );
  }

  void _go(BuildContext context, String location) {
    if (inDrawer) Navigator.of(context).pop();
    context.go(location);
  }
}

class _Brand extends StatelessWidget {
  const _Brand();

  @override
  Widget build(BuildContext context) {
    return const Padding(
      padding: EdgeInsets.fromLTRB(12, 3, 12, 16),
      child: Row(
        children: [
          OwnerBrandMark(),
          SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'حِصن',
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 21,
                    fontWeight: FontWeight.w800,
                    height: 1.15,
                  ),
                ),
                Text(
                  'نظام الأمان الذكي',
                  style: TextStyle(color: Color(0xFF90AAC5), fontSize: 10),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _SidebarNote extends StatelessWidget {
  const _SidebarNote();

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.only(top: 26),
      padding: const EdgeInsets.all(15),
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(13),
        border: Border.all(color: const Color(0xFF31506D)),
      ),
      child: const Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Padding(
            padding: EdgeInsets.only(top: 4),
            child: DecoratedBox(
              decoration: BoxDecoration(
                color: DesignTokens.success,
                shape: BoxShape.circle,
              ),
              child: SizedBox(width: 8, height: 8),
            ),
          ),
          SizedBox(width: 6),
          Expanded(
            child: Text(
              AdminCopy.noOwnerCommands,
              style: TextStyle(
                color: Color(0xFFADC3D7),
                fontSize: 11,
                height: 1.6,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _NavButton extends StatelessWidget {
  const _NavButton({
    required this.label,
    required this.icon,
    required this.selected,
    required this.onTap,
  });

  final String label;
  final IconData icon;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final color = selected ? DesignTokens.white : const Color(0xFFBED0E2);
    return Padding(
      padding: const EdgeInsets.only(bottom: 4),
      child: Material(
        color: selected ? DesignTokens.navy2 : Colors.transparent,
        borderRadius: BorderRadius.circular(10),
        child: InkWell(
          onTap: onTap,
          borderRadius: BorderRadius.circular(10),
          child: DecoratedBox(
            decoration: BoxDecoration(
              borderRadius: BorderRadius.circular(10),
              border: selected
                  ? const Border(
                      left: BorderSide(
                        color: DesignTokens.brandMarkStart,
                        width: 3,
                      ),
                    )
                  : null,
            ),
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 11),
              child: Row(
                children: [
                  Icon(icon, size: 18, color: color),
                  const SizedBox(width: 11),
                  Expanded(
                    child: Text(
                      label,
                      style: TextStyle(
                        color: color,
                        fontSize: 12,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
