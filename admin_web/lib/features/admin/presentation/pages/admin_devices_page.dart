import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/router/app_routes.dart';
import '../../domain/admin_ownership_view.dart';
import '../../domain/entities/admin_device_entity.dart';
import '../admin_copy.dart';
import '../admin_labels.dart';
import '../viewmodels/admin_devices_view_model.dart';
import '../widgets/admin_widgets.dart';

class AdminDevicesPage extends ConsumerStatefulWidget {
  const AdminDevicesPage({super.key});

  @override
  ConsumerState<AdminDevicesPage> createState() => _AdminDevicesPageState();
}

class _AdminDevicesPageState extends ConsumerState<AdminDevicesPage> {
  String _query = '';
  String _filter = AdminCopy.allStatuses;

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(adminDevicesViewModelProvider);
    final vm = ref.read(adminDevicesViewModelProvider.notifier);
    ref.listen(adminDevicesViewModelProvider, (prev, next) {
      final error = next.error;
      if (error == null || error == prev?.error) return;
      if (error.displayType != ErrorDisplayType.inline) {
        showErrorSnackBar(context, error);
      }
    });

    final q = _query.trim().toLowerCase();
    final rows = state.devices.where((device) {
      final online = device.indicators.connection.isOnline;
      if (_filter == AdminCopy.filterOnline && !online) return false;
      if (_filter == AdminCopy.filterOffline && online) return false;
      if (q.isEmpty) return true;
      return '${device.displayName} ${device.hardwareUid} ${device.ownerLabel}'
          .toLowerCase()
          .contains(q);
    }).toList();

    return AdminPageBody(
      title: AdminCopy.devices,
      subtitle: AdminCopy.devicesSubtitle,
      action: FilledButton(
        style: AdminButtonStyles.primary,
        onPressed: () => context.go(AppRoutes.adminDeviceRegister),
        child: const Text('⊕ ${AdminCopy.registerDevice}'),
      ),
      child: Column(
        children: [
          const AdminNotice(text: AdminCopy.offlineNote),
          const AdminNotice(
            text: AdminCopy.noOwnerCommands,
            tone: AdminNoticeTone.info,
          ),
          if (state.loading)
            const Padding(
              padding: EdgeInsets.all(32),
              child: Center(child: CircularProgressIndicator()),
            )
          else if (state.error != null && state.devices.isEmpty)
            MyEmptyState(
              type: MyEmptyType.error,
              title: context.tr.error,
              subtitle: state.error!.message,
              primaryActionLabel: context.tr.retry,
              onPrimaryAction: vm.load,
            )
          else if (state.devices.isEmpty)
            const MyEmptyState(
              type: MyEmptyType.empty,
              title: AdminCopy.emptyDevices,
              subtitle: AdminCopy.devicesSubtitle,
            )
          else
            AdminPanel(
              child: Column(
                children: [
                  AdminToolbar(
                    search: AdminSearchField(
                      hint: AdminCopy.searchHint,
                      onChanged: (value) => setState(() => _query = value),
                    ),
                    filter: AdminFilterMenu(
                      value: _filter,
                      options: const [
                        AdminCopy.allStatuses,
                        AdminCopy.filterOnline,
                        AdminCopy.filterOffline,
                      ],
                      onChanged: (value) => setState(() => _filter = value),
                    ),
                  ),
                  AdminTable(
                    headers: const [
                      AdminCopy.displayName,
                      AdminCopy.owner,
                      AdminCopy.connection,
                      AdminCopy.ownership,
                      AdminCopy.lastSeen,
                      '',
                    ],
                    rows: [
                      for (final device in rows)
                        [
                          _Name(device),
                          Text(device.ownerLabel),
                          _connection(context, device),
                          AdminPill(
                            adminOwnershipLabel(context, device.ownershipKind),
                            _tone(device.ownershipKind),
                          ),
                          Text(formatAdminTime(device.indicators.lastSeenAt)),
                          AdminTextLink(
                            label: AdminCopy.viewLink,
                            onPressed: () => context.go(
                              AppRoutes.adminDevicePath(device.deviceId),
                            ),
                          ),
                        ],
                    ],
                  ),
                ],
              ),
            ),
        ],
      ),
    );
  }
}

class _Name extends StatelessWidget {
  const _Name(this.device);

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

AdminPill _connection(BuildContext context, AdminDeviceEntity device) {
  final online = device.indicators.connection.isOnline;
  final tr = context.tr;
  return AdminPill(
    online ? tr.connectionOnline : tr.connectionOffline,
    online ? AdminPillTone.green : AdminPillTone.red,
  );
}

AdminPillTone _tone(AdminOwnershipKind kind) {
  return switch (kind) {
    AdminOwnershipKind.active ||
    AdminOwnershipKind.activeAwaitingAck => AdminPillTone.green,
    AdminOwnershipKind.pending => AdminPillTone.amber,
    AdminOwnershipKind.closed => AdminPillTone.red,
    AdminOwnershipKind.unlinked ||
    AdminOwnershipKind.unknown => AdminPillTone.gray,
  };
}
