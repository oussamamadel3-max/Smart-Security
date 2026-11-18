import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/router/app_routes.dart';
import '../../domain/admin_ownership_view.dart';
import '../../domain/entities/admin_device_entity.dart';
import '../../domain/entities/owner_account_entity.dart';
import '../admin_copy.dart';
import '../admin_labels.dart';
import '../viewmodels/admin_devices_view_model.dart';
import '../viewmodels/owner_accounts_view_model.dart';
import '../widgets/admin_widgets.dart';

class AdminDashboardPage extends ConsumerWidget {
  const AdminDashboardPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final accounts = ref.watch(ownerAccountsViewModelProvider);
    final devices = ref.watch(adminDevicesViewModelProvider);
    final offline = devices.devices
        .where((d) => !d.indicators.connection.isOnline)
        .length;
    final pending = devices.devices
        .where((d) => d.ownershipKind == AdminOwnershipKind.pending)
        .length;
    final loading = accounts.loading || devices.loading;

    return AdminPageBody(
      title: AdminCopy.dashboard,
      subtitle: AdminCopy.dashboardSubtitle,
      action: FilledButton(
        style: AdminButtonStyles.primary,
        onPressed: () => context.go(AppRoutes.adminDeviceRegister),
        child: const Text('⊕ ${AdminCopy.registerDevice}'),
      ),
      child: Column(
        children: [
          AdminMetricGrid(
            children: [
              AdminMetricCard(
                title: AdminCopy.accountsCount,
                value: '${accounts.accounts.length}',
                hint: AdminCopy.accountsHint,
                symbol: '♙',
              ),
              AdminMetricCard(
                title: AdminCopy.devicesCount,
                value: '${devices.devices.length}',
                hint: AdminCopy.devicesHint,
                symbol: '▣',
              ),
              AdminMetricCard(
                title: AdminCopy.offlineCount,
                value: '$offline',
                hint: AdminCopy.offlineHint,
                symbol: '◉',
              ),
              AdminMetricCard(
                title: AdminCopy.pendingCount,
                value: '$pending',
                hint: AdminCopy.pendingHint,
                symbol: '⚑',
              ),
            ],
          ),
          const SizedBox(height: 20),
          if (loading)
            const Padding(
              padding: EdgeInsets.all(24),
              child: Center(child: CircularProgressIndicator()),
            )
          else
            LayoutBuilder(
              builder: (context, constraints) {
                final stacked = constraints.maxWidth <= 1100;
                final accountPanel = _AccountPreview(accounts.accounts);
                final devicePanel = _DevicePreview(devices.devices);
                if (stacked) {
                  return Column(
                    children: [
                      devicePanel,
                      const SizedBox(height: 18),
                      accountPanel,
                    ],
                  );
                }
                return Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(flex: 3, child: devicePanel),
                    const SizedBox(width: 18),
                    Expanded(flex: 2, child: accountPanel),
                  ],
                );
              },
            ),
          const SizedBox(height: 8),
          const AdminNotice(
            text: AdminCopy.noOwnerCommands,
            tone: AdminNoticeTone.info,
          ),
          const AdminNotice(text: AdminCopy.auditPending),
        ],
      ),
    );
  }
}

class _AccountPreview extends StatelessWidget {
  const _AccountPreview(this.accounts);

  final List<OwnerAccountEntity> accounts;

  @override
  Widget build(BuildContext context) {
    final rows = accounts.take(5).toList();
    return AdminPanel(
      child: Column(
        children: [
          AdminSectionTitle(
            title: AdminCopy.accounts,
            action: AdminTextLink(
              label: AdminCopy.viewAccounts,
              onPressed: () => context.go(AppRoutes.adminAccounts),
            ),
          ),
          AdminTable(
            headers: const [AdminCopy.fullName, AdminCopy.status, ''],
            emptyLabel: AdminCopy.emptyAccounts,
            rows: [
              for (final account in rows)
                [
                  Text(
                    account.fullName,
                    style: const TextStyle(fontWeight: FontWeight.w700),
                  ),
                  adminAccountStatusPill(account.statusCode),
                  AdminTextLink(
                    label: AdminCopy.detailsLink,
                    onPressed: () =>
                        context.go(AppRoutes.adminAccountPath(account.id)),
                  ),
                ],
            ],
          ),
        ],
      ),
    );
  }
}

class _DevicePreview extends StatelessWidget {
  const _DevicePreview(this.devices);

  final List<AdminDeviceEntity> devices;

  @override
  Widget build(BuildContext context) {
    final rows = devices.take(5).toList();
    return AdminPanel(
      child: Column(
        children: [
          AdminSectionTitle(
            title: AdminCopy.devices,
            action: AdminTextLink(
              label: AdminCopy.viewDevices,
              onPressed: () => context.go(AppRoutes.adminDevices),
            ),
          ),
          AdminTable(
            headers: const [
              AdminCopy.displayName,
              AdminCopy.owner,
              AdminCopy.connection,
              AdminCopy.ownership,
              '',
            ],
            emptyLabel: AdminCopy.emptyDevices,
            rows: [
              for (final device in rows)
                [
                  _DeviceName(device),
                  Text(device.ownerLabel),
                  _connectionPill(context, device),
                  AdminPill(
                    adminOwnershipLabel(context, device.ownershipKind),
                    _ownershipTone(device.ownershipKind),
                  ),
                  AdminTextLink(
                    label: AdminCopy.viewLink,
                    onPressed: () =>
                        context.go(AppRoutes.adminDevicePath(device.deviceId)),
                  ),
                ],
            ],
          ),
        ],
      ),
    );
  }
}

class _DeviceName extends StatelessWidget {
  const _DeviceName(this.device);

  final AdminDeviceEntity device;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          device.displayName,
          style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 12),
        ),
        Text(
          device.hardwareUid,
          style: Theme.of(
            context,
          ).textTheme.labelSmall?.copyWith(color: DesignTokens.muted),
        ),
      ],
    );
  }
}

AdminPill _connectionPill(BuildContext context, AdminDeviceEntity device) {
  final online = device.indicators.connection.isOnline;
  final tr = context.tr;
  return AdminPill(
    online ? tr.connectionOnline : tr.connectionOffline,
    online ? AdminPillTone.green : AdminPillTone.red,
  );
}

AdminPillTone _ownershipTone(AdminOwnershipKind kind) {
  return switch (kind) {
    AdminOwnershipKind.active ||
    AdminOwnershipKind.activeAwaitingAck => AdminPillTone.green,
    AdminOwnershipKind.pending => AdminPillTone.amber,
    AdminOwnershipKind.closed => AdminPillTone.red,
    AdminOwnershipKind.unlinked ||
    AdminOwnershipKind.unknown => AdminPillTone.gray,
  };
}
