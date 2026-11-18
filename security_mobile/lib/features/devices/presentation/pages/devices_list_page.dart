import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/router/app_routes.dart';
import '../../../../core/session/auth_session_controller.dart';
import '../viewmodels/devices_list_view_model.dart';
import '../widgets/device_card.dart';

class DevicesListPage extends ConsumerWidget {
  const DevicesListPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final tr = context.tr;
    final state = ref.watch(devicesListViewModelProvider);
    final vm = ref.read(devicesListViewModelProvider.notifier);
    final user = ref.watch(authSessionProvider.select((s) => s.user));

    ref.listen(devicesListViewModelProvider, (prev, next) {
      final error = next.error;
      if (error == null || error == prev?.error) return;
      if (error.displayType == ErrorDisplayType.snackbar ||
          error.displayType == ErrorDisplayType.dialog) {
        showErrorSnackBar(context, error);
      }
    });

    return Scaffold(
      backgroundColor: DesignTokens.background,
      appBar: AppBar(
        title: Text(tr.myDevices),
        actions: [
          IconButton(
            tooltip: tr.refreshDevices,
            onPressed: state.isLoading ? null : vm.load,
            icon: state.isRefreshing
                ? const SizedBox(
                    width: 18,
                    height: 18,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  )
                : const Icon(Icons.refresh),
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: vm.load,
        child: ListView(
          padding: const EdgeInsets.all(DesignTokens.spaceLg),
          children: [
            Text(
              tr.myDevicesSubtitle,
              style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                    color: DesignTokens.muted,
                  ),
            ),
            if (user != null) ...[
              const SizedBox(height: DesignTokens.spaceXs),
              Text(
                user.fullName ?? user.userName ?? '',
                style: Theme.of(context).textTheme.labelMedium,
              ),
            ],
            const SizedBox(height: DesignTokens.spaceLg),
            Container(
              padding: const EdgeInsets.all(DesignTokens.spaceMd),
              decoration: BoxDecoration(
                color: DesignTokens.infoSoftBg,
                borderRadius: BorderRadius.circular(DesignTokens.radiusMd),
                border: Border.all(color: DesignTokens.border),
              ),
              child: Text(
                tr.pendingLinkNotice,
                style: Theme.of(context).textTheme.bodySmall?.copyWith(
                      color: DesignTokens.infoSoftText,
                    ),
              ),
            ),
            const SizedBox(height: DesignTokens.spaceLg),
            if (state.isLoading)
              const Padding(
                padding: EdgeInsets.symmetric(vertical: 48),
                child: Center(child: CircularProgressIndicator()),
              )
            else if (state.error != null && state.devices.isEmpty)
              MyEmptyState(
                type: MyEmptyType.error,
                title: tr.error,
                subtitle: state.error!.message,
                primaryActionLabel: tr.retry,
                onPrimaryAction: vm.load,
              )
            else if (state.isEmpty)
              MyEmptyState(
                type: MyEmptyType.empty,
                title: tr.noDevicesTitle,
                subtitle: tr.noDevicesSubtitle,
                primaryActionLabel: tr.refreshDevices,
                onPrimaryAction: vm.load,
              )
            else
              ...state.devices.map(
                (device) => Padding(
                  padding: const EdgeInsets.only(bottom: DesignTokens.spaceMd),
                  child: DeviceCard(
                    device: device,
                    onTap: device.showsLimitedPendingView
                        ? null
                        : () => context.push(
                              AppRoutes.deviceDetailPath(device.deviceId),
                            ),
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }
}
