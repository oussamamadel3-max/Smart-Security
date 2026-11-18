import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/router/app_routes.dart';
import '../../../../core/session/auth_session_controller.dart';
import '../../../../core/widgets/owner_visual.dart';
import '../viewmodels/devices_list_view_model.dart';
import '../widgets/device_card.dart';
import '../widgets/info_notice.dart';

/// Owner devices list. ACTIVE cards open device detail; PENDING is not tappable.
class DevicesListPage extends ConsumerWidget {
  const DevicesListPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final tr = context.tr;
    final state = ref.watch(devicesListViewModelProvider);
    final vm = ref.read(devicesListViewModelProvider.notifier);
    final user = ref.watch(authSessionProvider.select((s) => s.user));
    final width = MediaQuery.sizeOf(context).width;

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
        toolbarHeight: OwnerLayout.topBarHeight,
        centerTitle: false,
        title: Text(
          '${tr.appTitle} / ${OwnerVisualCopy.ownerCrumb}',
          style: Theme.of(context).textTheme.labelMedium,
        ),
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
          IconButton(
            tooltip: tr.logout,
            onPressed: () => ref.read(authSessionProvider.notifier).logout(),
            icon: const Icon(Icons.logout),
          ),
        ],
      ),
      body: Align(
        alignment: Alignment.topCenter,
        child: ConstrainedBox(
          constraints: const BoxConstraints(
            maxWidth: OwnerLayout.contentMaxWidth,
          ),
          child: RefreshIndicator(
            onRefresh: vm.load,
            child: ListView(
              padding: OwnerLayout.pagePadding(width),
              children: [
                OwnerPageHeader(
                  title: tr.myDevices,
                  subtitle: _homeSubtitle(tr, user?.fullName ?? user?.userName),
                ),
                InfoNotice(text: tr.pendingLinkNotice),
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
                  LayoutBuilder(
                    builder: (context, constraints) {
                      final columns = OwnerLayout.deviceColumns(width);
                      const gap = DesignTokens.spaceMd;
                      final itemWidth = columns == 1
                          ? constraints.maxWidth
                          : (constraints.maxWidth - gap) / 2;
                      return Wrap(
                        spacing: gap,
                        runSpacing: gap,
                        children: [
                          for (final device in state.devices)
                            SizedBox(
                              width: itemWidth,
                              child: DeviceCard(
                                device: device,
                                onTap:
                                    device.ownershipStatus ==
                                        OwnershipStatus.active
                                    ? () => context.push(
                                        AppRoutes.ownerDeviceDetailPath(
                                          device.deviceId,
                                        ),
                                      )
                                    : null,
                              ),
                            ),
                        ],
                      );
                    },
                  ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

String _homeSubtitle(AppLocalizations tr, String? name) {
  if (name == null || name.isEmpty) return tr.myDevicesSubtitle;
  return '$name — ${tr.myDevicesSubtitle}';
}
