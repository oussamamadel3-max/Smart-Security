import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/router/app_routes.dart';
import '../../domain/entities/owner_device_entity.dart';
import '../viewmodels/device_detail_view_model.dart';
import '../widgets/device_status_pills.dart';
import '../widgets/info_notice.dart';

class DeviceDetailPage extends ConsumerWidget {
  const DeviceDetailPage({super.key, required this.deviceId});

  final String deviceId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final tr = context.tr;
    final state = ref.watch(deviceDetailViewModelProvider(deviceId));
    final vm = ref.read(deviceDetailViewModelProvider(deviceId).notifier);
    final device = state.device;
    final theme = Theme.of(context);

    ref.listen(deviceDetailViewModelProvider(deviceId), (prev, next) {
      final error = next.error;
      if (error != null && error != prev?.error) {
        showErrorSnackBar(context, error);
      }
      final info = next.infoMessage;
      if (info != null && info != prev?.infoMessage) {
        showMySnackBar(context, message: info, type: MySnackType.info);
      }
    });

    return Scaffold(
      backgroundColor: DesignTokens.background,
      appBar: AppBar(
        title: Text(tr.deviceDetails),
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => context.go(AppRoutes.devices),
        ),
        actions: [
          IconButton(
            tooltip: tr.refresh,
            onPressed: () => vm.load(deviceId),
            icon: const Icon(Icons.refresh),
          ),
        ],
      ),
      body: state.isLoading && device == null
          ? const Center(child: CircularProgressIndicator())
          : state.error != null && device == null
              ? MyEmptyState(
                  type: MyEmptyType.error,
                  title: tr.error,
                  subtitle: state.error!.message,
                  primaryActionLabel: tr.retry,
                  onPrimaryAction: () => vm.load(deviceId),
                )
              : device == null
                  ? MyEmptyState(
                      type: MyEmptyType.empty,
                      title: tr.noDevicesTitle,
                      subtitle: tr.noDevicesSubtitle,
                    )
                  : ListView(
                      padding: const EdgeInsets.all(DesignTokens.spaceLg),
                      children: [
                        _HeroHeader(device: device),
                        const SizedBox(height: DesignTokens.spaceLg),
                        if (device.showsLimitedPendingView)
                          InfoNotice(text: tr.pendingLinkNotice)
                        else ...[
                          if (device.indicators?.showsStaleWarning == true)
                            InfoNotice(
                              text: tr.staleWhileOffline,
                              tone: InfoNoticeTone.warning,
                            ),
                          if (device.isActiveAwaitingOwnershipSync)
                            InfoNotice(
                              text: device.canControl
                                  ? tr.commandHoldUntilSync
                                  : tr.cannotControlYet,
                              tone: InfoNoticeTone.warning,
                            ),
                          if (state.pendingCommand != null)
                            InfoNotice(
                              text: tr.pendingCommandBanner(
                                state.pendingCommand!.type.code,
                                state.pendingCommand!.status.code,
                              ),
                            ),
                          const SizedBox(height: DesignTokens.spaceMd),
                          Text(
                            tr.lastConfirmedState,
                            style: theme.textTheme.titleSmall,
                          ),
                          const SizedBox(height: DesignTokens.spaceMd),
                          Wrap(
                            spacing: DesignTokens.spaceSm,
                            runSpacing: DesignTokens.spaceSm,
                            children: DeviceStatusLabels.indicatorPills(
                              context,
                              device.indicators!,
                            ),
                          ),
                          if (device.indicators?.lastStateAt != null) ...[
                            const SizedBox(height: DesignTokens.spaceLg),
                            Text(
                              '${tr.lastConfirmedState}: ${device.indicators!.lastStateAt}',
                              style: theme.textTheme.bodySmall?.copyWith(
                                color: DesignTokens.muted,
                              ),
                            ),
                          ],
                          const SizedBox(height: DesignTokens.spaceXl),
                          OutlinedButton.icon(
                            onPressed: () => context.go(
                              AppRoutes.deviceHistoryPath(deviceId),
                            ),
                            icon: const Icon(Icons.history),
                            label: Text(tr.viewDeviceHistory),
                          ),
                          const SizedBox(height: DesignTokens.spaceMd),
                          OutlinedButton.icon(
                            onPressed: () => context.go(
                              AppRoutes.deviceWifiSetupPath(deviceId),
                            ),
                            icon: const Icon(Icons.wifi),
                            label: Text(tr.wifiSetupTitle),
                          ),
                          const SizedBox(height: DesignTokens.spaceXl),
                          Text(
                            tr.deviceCommandsSection,
                            style: theme.textTheme.titleSmall,
                          ),
                          const SizedBox(height: DesignTokens.spaceMd),
                          _CommandButtons(
                            enabled: device.canControl &&
                                !state.isSubmitting &&
                                !state.hasUnresolvedTimeout &&
                                device.indicators?.connection.isOnline == true,
                            submitting: state.isSubmitting,
                            onArm: () => vm.submitCommand(CommandType.arm),
                            onDisarm: () =>
                                vm.submitCommand(CommandType.disarm),
                            onMute: () =>
                                vm.submitCommand(CommandType.muteAlarm),
                            onTest: () =>
                                vm.submitCommand(CommandType.testSiren),
                            onStopTest: () =>
                                vm.submitCommand(CommandType.stopTest),
                            muteEnabled:
                                device.indicators?.currentAlarmEventUid != null,
                            testEnabled:
                                device.indicators?.siren != SirenStatus.alarm,
                            stopTestEnabled:
                                device.indicators?.siren == SirenStatus.test,
                          ),
                          if (!device.canControl ||
                              device.indicators?.connection.isOnline != true)
                            Padding(
                              padding: const EdgeInsets.only(
                                top: DesignTokens.spaceMd,
                              ),
                              child: Text(
                                device.canControl
                                    ? tr.deviceOfflineNoLiveControl
                                    : tr.cannotControlYet,
                                style: theme.textTheme.bodySmall?.copyWith(
                                  color: DesignTokens.muted,
                                ),
                              ),
                            ),
                        ],
                      ],
                    ),
    );
  }
}

class _HeroHeader extends StatelessWidget {
  const _HeroHeader({required this.device});

  final OwnerDeviceEntity device;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Container(
      padding: const EdgeInsets.all(DesignTokens.spaceXl),
      decoration: BoxDecoration(
        gradient: const LinearGradient(
          colors: [DesignTokens.navy, DesignTokens.navy2],
          begin: Alignment.topRight,
          end: Alignment.bottomLeft,
        ),
        borderRadius: BorderRadius.circular(DesignTokens.radiusXl),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            device.deviceId,
            style: theme.textTheme.labelMedium?.copyWith(
              color: const Color(0xFF98C7E5),
            ),
          ),
          const SizedBox(height: DesignTokens.spaceSm),
          Text(
            device.displayName,
            style: theme.textTheme.headlineSmall?.copyWith(
              color: DesignTokens.white,
            ),
          ),
          const SizedBox(height: DesignTokens.spaceMd),
          Wrap(
            spacing: DesignTokens.spaceSm,
            runSpacing: DesignTokens.spaceSm,
            children: [
              DeviceStatusLabels.ownership(context, device),
              if (device.showsOperationalIndicators)
                DeviceStatusLabels.connection(
                  context,
                  device.indicators!.connection,
                ),
            ],
          ),
        ],
      ),
    );
  }
}

class _CommandButtons extends StatelessWidget {
  const _CommandButtons({
    required this.enabled,
    required this.submitting,
    required this.onArm,
    required this.onDisarm,
    required this.onMute,
    required this.onTest,
    required this.onStopTest,
    required this.muteEnabled,
    required this.testEnabled,
    required this.stopTestEnabled,
  });

  final bool enabled;
  final bool submitting;
  final VoidCallback onArm;
  final VoidCallback onDisarm;
  final VoidCallback onMute;
  final VoidCallback onTest;
  final VoidCallback onStopTest;
  final bool muteEnabled;
  final bool testEnabled;
  final bool stopTestEnabled;

  @override
  Widget build(BuildContext context) {
    final tr = context.tr;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Row(
          children: [
            Expanded(
              child: ElevatedButton(
                onPressed: enabled && !submitting ? onArm : null,
                child: Text(tr.commandArm),
              ),
            ),
            const SizedBox(width: DesignTokens.spaceMd),
            Expanded(
              child: OutlinedButton(
                onPressed: enabled && !submitting ? onDisarm : null,
                child: Text(tr.commandDisarm),
              ),
            ),
          ],
        ),
        const SizedBox(height: DesignTokens.spaceMd),
        ElevatedButton(
          onPressed: enabled && muteEnabled && !submitting ? onMute : null,
          child: Text(tr.commandMute),
        ),
        const SizedBox(height: DesignTokens.spaceSm),
        Row(
          children: [
            Expanded(
              child: OutlinedButton(
                onPressed: enabled && testEnabled && !submitting ? onTest : null,
                child: Text(tr.commandTestSiren),
              ),
            ),
            const SizedBox(width: DesignTokens.spaceMd),
            Expanded(
              child: OutlinedButton(
                onPressed: enabled && stopTestEnabled && !submitting
                    ? onStopTest
                    : null,
                child: Text(tr.commandStopTest),
              ),
            ),
          ],
        ),
        if (submitting)
          const Padding(
            padding: EdgeInsets.only(top: DesignTokens.spaceMd),
            child: Center(child: CircularProgressIndicator()),
          ),
      ],
    );
  }
}
