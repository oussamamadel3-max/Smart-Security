import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/router/app_route_observer.dart';
import '../../../../core/router/app_routes.dart';
import '../../../../core/widgets/owner_visual.dart';
import '../../domain/entities/owner_device_entity.dart';
import '../viewmodels/device_detail_view_model.dart';
import '../widgets/device_command_bar.dart';
import '../widgets/device_status_pills.dart';
import '../widgets/info_notice.dart';
import '../widgets/state_metric_card.dart';

class DeviceDetailPage extends ConsumerStatefulWidget {
  const DeviceDetailPage({super.key, required this.deviceId});

  final String deviceId;

  @override
  ConsumerState<DeviceDetailPage> createState() => _DeviceDetailPageState();
}

class _DeviceDetailPageState extends ConsumerState<DeviceDetailPage>
    with WidgetsBindingObserver, RouteAware {
  bool _routeSubscribed = false;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (_routeSubscribed) return;
    final route = ModalRoute.of(context);
    if (route == null) return;
    appRouteObserver.subscribe(this, route);
    _routeSubscribed = true;
  }

  @override
  void dispose() {
    _poll(pause: true);
    WidgetsBinding.instance.removeObserver(this);
    if (_routeSubscribed) {
      appRouteObserver.unsubscribe(this);
    }
    super.dispose();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    switch (state) {
      case AppLifecycleState.resumed:
        _reload();
        _poll(pause: false);
        break;
      case AppLifecycleState.hidden:
      case AppLifecycleState.paused:
        _poll(pause: true);
        break;
      case AppLifecycleState.inactive:
      case AppLifecycleState.detached:
        break;
    }
  }

  @override
  void didPushNext() {
    _poll(pause: true);
  }

  @override
  void didPopNext() {
    _reload();
    _poll(pause: false);
  }

  void _reload() {
    if (!mounted) return;
    ref
        .read(deviceDetailViewModelProvider(widget.deviceId).notifier)
        .load(widget.deviceId);
  }

  void _poll({required bool pause}) {
    final vm = ref.read(
      deviceDetailViewModelProvider(widget.deviceId).notifier,
    );
    if (pause) {
      vm.pausePolling();
      return;
    }
    vm.resumePollingIfNeeded();
  }

  @override
  Widget build(BuildContext context) {
    final tr = context.tr;
    final deviceId = widget.deviceId;
    final state = ref.watch(deviceDetailViewModelProvider(deviceId));
    final vm = ref.read(deviceDetailViewModelProvider(deviceId).notifier);
    final device = state.device;
    final theme = Theme.of(context);
    final width = MediaQuery.sizeOf(context).width;

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
        toolbarHeight: OwnerLayout.topBarHeight,
        centerTitle: false,
        title: Text(
          '${tr.appTitle} / ${OwnerVisualCopy.ownerCrumb}',
          style: Theme.of(context).textTheme.labelMedium,
        ),
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => context.go(AppRoutes.ownerHome),
        ),
        actions: [
          IconButton(
            tooltip: tr.refresh,
            onPressed: () => vm.load(deviceId),
            icon: const Icon(Icons.refresh),
          ),
        ],
      ),
      body: Align(
        alignment: Alignment.topCenter,
        child: ConstrainedBox(
          constraints: const BoxConstraints(
            maxWidth: OwnerLayout.contentMaxWidth,
          ),
          child: state.isLoading && device == null
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
                  padding: OwnerLayout.pagePadding(width),
                  children: [
                    OwnerPageHeader(
                      title: tr.deviceDetails,
                      subtitle: OwnerVisualCopy.deviceDetailSubtitle,
                    ),
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
                      StateMetricGrid(
                        children: [
                          StateMetricCard(
                            title: OwnerVisualCopy.connectionTitle,
                            value: _connectionValue(tr, device),
                            hint: device.indicators?.lastSeenAt == null
                                ? tr.lastSeen
                                : '${tr.lastSeen}: ${device.indicators!.lastSeenAt}',
                            symbol: '◉',
                          ),
                          StateMetricCard(
                            title: OwnerVisualCopy.protectionTitle,
                            value: _armValue(device),
                            hint: OwnerVisualCopy.lastAckHint,
                            symbol: '♜',
                          ),
                          StateMetricCard(
                            title: OwnerVisualCopy.sirenTitle,
                            value: _sirenValue(device),
                            hint: OwnerVisualCopy.sirenHint,
                            symbol: '◖',
                          ),
                          StateMetricCard(
                            title: OwnerVisualCopy.hardwareTitle,
                            value: _healthValue(device),
                            hint: OwnerVisualCopy.hardwareHint,
                            symbol: '⚑',
                          ),
                        ],
                      ),
                      const SizedBox(height: DesignTokens.spaceLg),
                      _ControlPanel(
                        theme: theme,
                        child: DeviceCommandBar(
                          enabled:
                              device.canControl &&
                              !state.isSubmitting &&
                              !state.hasUnresolvedTimeout &&
                              device.indicators?.connection.isOnline == true,
                          submitting: state.isSubmitting,
                          onArm: () => vm.submitCommand(CommandType.arm),
                          onDisarm: () => vm.submitCommand(CommandType.disarm),
                          onMute: () => vm.submitCommand(CommandType.muteAlarm),
                          onTest: () => vm.submitCommand(CommandType.testSiren),
                          onStopTest: () =>
                              vm.submitCommand(CommandType.stopTest),
                          muteEnabled:
                              device.indicators?.currentAlarmEventUid != null,
                          testEnabled:
                              device.indicators?.siren != SirenStatus.alarm,
                          stopTestEnabled:
                              device.indicators?.siren == SirenStatus.test,
                        ),
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
                      Align(
                        alignment: AlignmentDirectional.centerStart,
                        child: TextButton(
                          onPressed: () => context.go(
                            AppRoutes.ownerDeviceHistoryPath(deviceId),
                          ),
                          child: Text(tr.viewDeviceHistory),
                        ),
                      ),
                    ],
                  ],
                ),
        ),
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
        borderRadius: BorderRadius.circular(17),
      ),
      child: LayoutBuilder(
        builder: (context, constraints) {
          final compact = constraints.maxWidth < 420;
          return Row(
            crossAxisAlignment: CrossAxisAlignment.center,
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      device.deviceId,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      style: theme.textTheme.labelMedium?.copyWith(
                        color: const Color(0xFF98C7E5),
                      ),
                    ),
                    const SizedBox(height: DesignTokens.spaceSm),
                    Text(
                      device.displayName,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      style: theme.textTheme.headlineMedium?.copyWith(
                        color: DesignTokens.white,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                    if (device.linkStatusMessage.isNotEmpty) ...[
                      const SizedBox(height: DesignTokens.spaceXs),
                      Text(
                        device.linkStatusMessage,
                        maxLines: 3,
                        overflow: TextOverflow.ellipsis,
                        style: theme.textTheme.labelMedium?.copyWith(
                          color: const Color(0xFFB6CFDF),
                        ),
                      ),
                    ],
                    const SizedBox(height: DesignTokens.spaceMd),
                    Wrap(
                      spacing: DesignTokens.spaceSm,
                      runSpacing: DesignTokens.spaceSm,
                      children: [
                        DeviceStatusLabels.ownership(context, device),
                        if (device.showsOperationalIndicators) ...[
                          DeviceStatusLabels.connection(
                            context,
                            device.indicators!.connection,
                          ),
                          const StatusPill(
                            label: OwnerVisualCopy.confirmedState,
                            tone: StatusPillTone.blue,
                          ),
                        ],
                      ],
                    ),
                  ],
                ),
              ),
              Text(
                '♜',
                style: TextStyle(
                  fontSize: compact ? 36 : 64,
                  color: DesignTokens.white,
                ),
              ),
            ],
          );
        },
      ),
    );
  }
}

class _ControlPanel extends StatelessWidget {
  const _ControlPanel({required this.theme, required this.child});

  final ThemeData theme;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      margin: const EdgeInsets.only(top: DesignTokens.spaceLg),
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: DesignTokens.card,
        borderRadius: BorderRadius.circular(DesignTokens.radiusXl),
        border: Border.all(color: DesignTokens.border),
        boxShadow: DesignTokens.cardShadow,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(OwnerVisualCopy.controlTitle, style: theme.textTheme.titleLarge),
          const SizedBox(height: DesignTokens.spaceMd),
          Text(
            OwnerVisualCopy.commandsHint,
            style: theme.textTheme.bodySmall?.copyWith(
              color: DesignTokens.muted,
            ),
          ),
          const SizedBox(height: DesignTokens.spaceLg),
          child,
        ],
      ),
    );
  }
}

String _connectionValue(AppLocalizations tr, OwnerDeviceEntity device) {
  return switch (device.indicators?.connection) {
    ConnectionStatus.online => tr.connectionOnline,
    ConnectionStatus.offline => tr.connectionOffline,
    _ => OwnerVisualCopy.unknownShort,
  };
}

String _armValue(OwnerDeviceEntity device) {
  return switch (device.indicators?.arm) {
    ArmStatus.armed => OwnerVisualCopy.armedShort,
    ArmStatus.disarmed => OwnerVisualCopy.disarmedShort,
    _ => OwnerVisualCopy.unknownShort,
  };
}

String _sirenValue(OwnerDeviceEntity device) {
  return switch (device.indicators?.siren) {
    SirenStatus.off => OwnerVisualCopy.sirenOffShort,
    SirenStatus.alarm => OwnerVisualCopy.sirenAlarmShort,
    SirenStatus.test => OwnerVisualCopy.sirenTestShort,
    _ => OwnerVisualCopy.unknownShort,
  };
}

String _healthValue(OwnerDeviceEntity device) {
  return switch (device.indicators?.health) {
    DiagnosticStatus.healthy => OwnerVisualCopy.healthyShort,
    DiagnosticStatus.fault => OwnerVisualCopy.faultShort,
    _ => OwnerVisualCopy.unknownShort,
  };
}
