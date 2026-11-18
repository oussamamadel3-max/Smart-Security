import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/router/app_routes.dart';
import '../../../../core/session/auth_session_controller.dart';
import '../../../devices/domain/entities/owner_device_entity.dart';
import '../../../devices/presentation/viewmodels/devices_list_view_model.dart';
import '../../../devices/presentation/widgets/device_card.dart';

/// Owner dashboard — metrics and device previews from live list API only.
class DashboardPage extends ConsumerWidget {
  const DashboardPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final tr = context.tr;
    final theme = Theme.of(context);
    final user = ref.watch(authSessionProvider.select((s) => s.user));
    final state = ref.watch(devicesListViewModelProvider);
    final vm = ref.read(devicesListViewModelProvider.notifier);
    final devices = state.devices;

    final online = devices
        .where((d) => d.indicators?.connection.isOnline == true)
        .length;
    final armed = devices
        .where((d) => d.indicators?.arm == ArmStatus.armed)
        .length;
    final greetingName = user?.fullName ?? user?.userName ?? '';

    return Scaffold(
      backgroundColor: DesignTokens.background,
      appBar: AppBar(
        title: Text(tr.navDashboard),
        actions: [
          IconButton(
            tooltip: tr.refresh,
            onPressed: state.isLoading ? null : vm.load,
            icon: const Icon(Icons.refresh),
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: vm.load,
        child: ListView(
          padding: const EdgeInsets.all(DesignTokens.spaceLg),
          children: [
            Text(
              greetingName.isEmpty
                  ? tr.dashboardGreetingGeneric
                  : tr.dashboardGreeting(greetingName),
              style: theme.textTheme.headlineSmall,
            ),
            const SizedBox(height: DesignTokens.spaceSm),
            Text(
              tr.dashboardSubtitle,
              style: theme.textTheme.bodyMedium?.copyWith(
                color: DesignTokens.muted,
              ),
            ),
            const SizedBox(height: DesignTokens.spaceLg),
            if (state.isLoading && devices.isEmpty)
              const Padding(
                padding: EdgeInsets.all(DesignTokens.spaceXl),
                child: Center(child: CircularProgressIndicator()),
              )
            else if (state.error != null && devices.isEmpty)
              MyEmptyState(
                type: MyEmptyType.error,
                title: tr.error,
                subtitle: state.error!.message,
                primaryActionLabel: tr.retry,
                onPrimaryAction: vm.load,
              )
            else ...[
              LayoutBuilder(
                builder: (context, constraints) {
                  final wide = constraints.maxWidth >= 520;
                  final cards = [
                    _MetricCard(
                      label: tr.metricTotalDevices,
                      value: '${devices.length}',
                      icon: Icons.devices_other,
                    ),
                    _MetricCard(
                      label: tr.metricOnlineDevices,
                      value: '$online',
                      icon: Icons.wifi,
                    ),
                    _MetricCard(
                      label: tr.metricArmedDevices,
                      value: '$armed',
                      icon: Icons.shield_outlined,
                    ),
                    _MetricCard(
                      label: tr.metricPendingLinks,
                      value:
                          '${devices.where((d) => d.isInitialLinkPending).length}',
                      icon: Icons.link,
                    ),
                  ];
                  if (wide) {
                    return Wrap(
                      spacing: DesignTokens.spaceMd,
                      runSpacing: DesignTokens.spaceMd,
                      children: cards
                          .map(
                            (c) => SizedBox(
                              width: (constraints.maxWidth -
                                      DesignTokens.spaceMd) /
                                  2,
                              child: c,
                            ),
                          )
                          .toList(),
                    );
                  }
                  return Column(
                    children: [
                      for (final c in cards) ...[
                        c,
                        const SizedBox(height: DesignTokens.spaceMd),
                      ],
                    ],
                  );
                },
              ),
              const SizedBox(height: DesignTokens.spaceLg),
              Row(
                children: [
                  Expanded(
                    child: Text(
                      tr.myDevices,
                      style: theme.textTheme.titleSmall,
                    ),
                  ),
                  TextButton(
                    onPressed: () => context.go(AppRoutes.devices),
                    child: Text(tr.viewAllDevices),
                  ),
                ],
              ),
              const SizedBox(height: DesignTokens.spaceMd),
              if (devices.isEmpty)
                MyEmptyState(
                  type: MyEmptyType.empty,
                  title: tr.noDevicesTitle,
                  subtitle: tr.noDevicesSubtitle,
                )
              else
                ..._previewDevices(context, devices),
              const SizedBox(height: DesignTokens.spaceLg),
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(DesignTokens.spaceMd),
                decoration: BoxDecoration(
                  color: DesignTokens.infoSoftBg,
                  borderRadius: BorderRadius.circular(DesignTokens.radiusMd),
                  border: Border.all(color: DesignTokens.border),
                ),
                child: Text(
                  tr.dashboardConfirmedStateNotice,
                  style: theme.textTheme.bodySmall?.copyWith(
                    color: DesignTokens.infoSoftText,
                  ),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }

  List<Widget> _previewDevices(
    BuildContext context,
    List<OwnerDeviceEntity> devices,
  ) {
    final preview = devices.take(2).toList(growable: false);
    return [
      for (final device in preview) ...[
        DeviceCard(
          device: device,
          onTap: device.showsLimitedPendingView
              ? null
              : () => context.push(AppRoutes.deviceDetailPath(device.deviceId)),
        ),
        const SizedBox(height: DesignTokens.spaceMd),
      ],
    ];
  }
}

class _MetricCard extends StatelessWidget {
  const _MetricCard({
    required this.label,
    required this.value,
    required this.icon,
  });

  final String label;
  final String value;
  final IconData icon;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(DesignTokens.spaceLg),
      decoration: BoxDecoration(
        color: DesignTokens.card,
        borderRadius: BorderRadius.circular(DesignTokens.radiusLg),
        border: Border.all(color: DesignTokens.border),
        boxShadow: const [
          BoxShadow(
            color: Color(0x0E203855),
            blurRadius: 28,
            offset: Offset(0, 8),
          ),
        ],
      ),
      child: Row(
        children: [
          Icon(icon, color: DesignTokens.primary),
          const SizedBox(width: DesignTokens.spaceMd),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  label,
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(
                        color: DesignTokens.muted,
                      ),
                ),
                Text(
                  value,
                  style: Theme.of(context).textTheme.headlineSmall,
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
