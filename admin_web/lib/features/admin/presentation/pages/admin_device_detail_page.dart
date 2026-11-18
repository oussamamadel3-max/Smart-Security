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

class AdminDeviceDetailPage extends ConsumerWidget {
  const AdminDeviceDetailPage({super.key, required this.deviceId});

  final String deviceId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final provider = adminDeviceDetailViewModelProvider(deviceId);
    final state = ref.watch(provider);
    final device = state.device;
    ref.listen(provider, (prev, next) {
      final error = next.write.error;
      if (error != null &&
          error != prev?.write.error &&
          error.displayType != ErrorDisplayType.inline) {
        showErrorSnackBar(context, error);
      }
    });

    return AdminPageBody(
      title: AdminCopy.deviceDetail,
      subtitle: AdminCopy.devicesSubtitle,
      action: OutlinedButton(
        style: AdminButtonStyles.outline,
        onPressed: () => context.go(AppRoutes.adminDevices),
        child: const Text('← ${AdminCopy.back}'),
      ),
      child: state.loading && device == null
          ? const Center(child: CircularProgressIndicator())
          : device == null
          ? MyEmptyState(
              type: MyEmptyType.error,
              title: context.tr.error,
              subtitle: state.write.error?.message ?? '',
              primaryActionLabel: context.tr.retry,
              onPrimaryAction: () => ref.read(provider.notifier).load(),
            )
          : AdminPanel(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  AdminSectionTitle(
                    title: '${device.displayName} — ${device.hardwareUid}',
                    action: AdminPill(
                      adminOwnershipLabel(context, device.ownershipKind),
                      _tone(device.ownershipKind),
                    ),
                  ),
                  AdminNotice(text: adminDeviceBanner(device)),
                  const AdminNotice(
                    text: AdminCopy.noOwnerCommands,
                    tone: AdminNoticeTone.info,
                  ),
                  AdminKvGrid(
                    entries: [
                      (AdminCopy.owner, device.ownerLabel),
                      (AdminCopy.lifecycle, device.lifecycleStatus),
                      (AdminCopy.version, '${device.ownershipVersion ?? '—'}'),
                      (
                        AdminCopy.confirmedVersion,
                        '${device.confirmedOwnershipVersion}',
                      ),
                      (AdminCopy.connection, _connection(context, device)),
                      (AdminCopy.arm, _arm(context, device)),
                      (AdminCopy.siren, _siren(context, device)),
                      (AdminCopy.health, _health(context, device)),
                      (
                        AdminCopy.lastSeen,
                        formatAdminTime(device.indicators.lastSeenAt),
                      ),
                      (AdminCopy.sensor, AdminCopy.sensorValue),
                    ],
                  ),
                  if (!device.indicators.clockTrusted)
                    Padding(
                      padding: const EdgeInsets.only(top: 12),
                      child: Text(context.tr.occurrenceUntrusted),
                    ),
                  const SizedBox(height: 12),
                  const AdminNotice(text: AdminCopy.offlineNote),
                  if (state.write.error != null)
                    AdminNotice(text: state.write.error!.message, danger: true),
                  if (state.unlinkResult != null)
                    AdminNotice(
                      text:
                          '${AdminCopy.unlink}: ${state.unlinkResult!.closedOwnershipStatus.code}. ${state.unlinkResult!.ownershipSyncQueued ? AdminCopy.syncQueued : ''}',
                    ),
                  if (state.archiveResult != null)
                    AdminNotice(
                      text:
                          '${AdminCopy.archive}: ${state.archiveResult!.lifecycleStatus}. ${state.archiveResult!.ownershipSyncQueued ? AdminCopy.syncQueued : ''}',
                    ),
                  AdminCommandRow(
                    children: [
                      if (device.canOfferLink)
                        FilledButton(
                          style: AdminButtonStyles.primary,
                          onPressed: () => context.go(
                            AppRoutes.adminDeviceLinkPath(device.deviceId),
                          ),
                          child: const Text(AdminCopy.link),
                        ),
                      if (device.canOfferTransfer)
                        FilledButton(
                          style: AdminButtonStyles.secondary,
                          onPressed: () => context.go(
                            AppRoutes.adminDeviceTransferPath(device.deviceId),
                          ),
                          child: const Text(AdminCopy.transfer),
                        ),
                      if (device.canOfferUnlink)
                        FilledButton(
                          style: AdminButtonStyles.danger,
                          onPressed: state.write.busy
                              ? null
                              : () => _confirm(
                                  context,
                                  AdminCopy.unlink,
                                  AdminCopy.unlinkConfirm,
                                  () => ref.read(provider.notifier).unlink(),
                                ),
                          child: const Text(AdminCopy.unlink),
                        ),
                      if (device.canOfferArchive)
                        OutlinedButton(
                          style: AdminButtonStyles.outline,
                          onPressed: state.write.busy
                              ? null
                              : () => _confirm(
                                  context,
                                  AdminCopy.archive,
                                  AdminCopy.archiveDeviceConfirm,
                                  () => ref.read(provider.notifier).archive(),
                                ),
                          child: const Text(AdminCopy.archive),
                        ),
                      OutlinedButton(
                        style: AdminButtonStyles.outline,
                        onPressed: () => context.go(AppRoutes.adminAuditLogs),
                        child: const Text(AdminCopy.auditLogs),
                      ),
                    ],
                  ),
                ],
              ),
            ),
    );
  }

  String _connection(BuildContext context, AdminDeviceEntity device) {
    final tr = context.tr;
    return device.indicators.connection.isOnline
        ? tr.connectionOnline
        : tr.connectionOffline;
  }

  String _arm(BuildContext context, AdminDeviceEntity device) {
    final tr = context.tr;
    return switch (device.indicators.arm) {
      ArmStatus.armed => tr.armArmed,
      ArmStatus.disarmed => tr.armDisarmed,
      ArmStatus.unknown => tr.armUnknown,
    };
  }

  String _siren(BuildContext context, AdminDeviceEntity device) {
    final tr = context.tr;
    return switch (device.indicators.siren) {
      SirenStatus.alarm => tr.sirenAlarm,
      SirenStatus.test => tr.sirenTest,
      SirenStatus.off => tr.sirenOff,
      SirenStatus.unknown => tr.sirenUnknown,
    };
  }

  String _health(BuildContext context, AdminDeviceEntity device) {
    final tr = context.tr;
    return switch (device.indicators.health) {
      DiagnosticStatus.healthy => tr.healthHealthy,
      DiagnosticStatus.fault => tr.healthFault,
      DiagnosticStatus.unknown => tr.healthUnknown,
    };
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

  Future<void> _confirm(
    BuildContext context,
    String title,
    String message,
    Future<void> Function() action,
  ) async {
    final ok = await showAdminConfirmDialog(
      context: context,
      title: title,
      message: message,
      danger: true,
    );
    if (ok == true) await action();
  }
}
