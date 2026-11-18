import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/router/app_routes.dart';
import '../../../../core/widgets/owner_visual.dart';
import '../../domain/history_query.dart';
import '../history_format.dart';
import '../viewmodels/device_history_view_models.dart';
import '../widgets/device_status_pills.dart';
import '../widgets/history_kv_row.dart';
import '../widgets/info_notice.dart';

class CommandDetailPage extends ConsumerWidget {
  const CommandDetailPage({
    super.key,
    required this.deviceId,
    required this.commandId,
  });

  final String deviceId;
  final String commandId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final tr = context.tr;
    final key = CommandDetailArgs(deviceId: deviceId, commandId: commandId);
    final state = ref.watch(commandDetailViewModelProvider(key));
    final vm = ref.read(commandDetailViewModelProvider(key).notifier);
    final command = state.command;
    final width = MediaQuery.sizeOf(context).width;

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
          onPressed: () => context.go(
            AppRoutes.ownerDeviceHistoryPath(deviceId, tab: 'commands'),
          ),
        ),
        actions: [
          IconButton(
            tooltip: tr.refresh,
            onPressed: vm.load,
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
          child: state.isLoading && command == null
              ? const Center(child: CircularProgressIndicator())
              : state.error != null && command == null
              ? MyEmptyState(
                  type: MyEmptyType.error,
                  title: tr.error,
                  subtitle: state.error!.message,
                  primaryActionLabel: tr.retry,
                  onPrimaryAction: vm.load,
                )
              : command == null
              ? MyEmptyState(
                  type: MyEmptyType.empty,
                  title: tr.noCommandsTitle,
                  subtitle: tr.noCommandsSubtitle,
                )
              : ListView(
                  padding: OwnerLayout.pagePadding(width),
                  children: [
                    OwnerPageHeader(
                      title: tr.commandDetailTitle,
                      subtitle: OwnerVisualCopy.commandDetailSubtitle,
                    ),
                    if (command.status == CommandStatus.timedOut)
                      InfoNotice(
                        text: tr.timedOutUnknownNotice,
                        tone: InfoNoticeTone.warning,
                      ),
                    Container(
                      padding: const EdgeInsets.all(20),
                      decoration: BoxDecoration(
                        color: DesignTokens.card,
                        borderRadius: BorderRadius.circular(
                          DesignTokens.radiusXl,
                        ),
                        border: Border.all(color: DesignTokens.border),
                        boxShadow: DesignTokens.cardShadow,
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            children: [
                              Expanded(
                                child: Text(
                                  commandHistoryTitle(tr, command),
                                  style: Theme.of(context).textTheme.titleLarge,
                                ),
                              ),
                              DeviceStatusLabels.commandStatus(command.status),
                            ],
                          ),
                          const SizedBox(height: DesignTokens.spaceLg),
                          HistoryKvGrid(
                            rows: [
                              HistoryKvRow(
                                label: tr.commandTypeLabel,
                                value: isSirenTestCommand(command)
                                    ? commandHistoryTitle(tr, command)
                                    : command.type.code,
                              ),
                              HistoryKvRow(
                                label: tr.statusLabel,
                                value: command.status.code,
                              ),
                              HistoryKvRow(
                                label: tr.createdAtLabel,
                                value: formatHistoryTime(command.createdAt),
                              ),
                              HistoryKvRow(
                                label: tr.expiresAtLabel,
                                value: formatHistoryTime(command.expiresAt),
                              ),
                              if (command.rejectCode != null)
                                HistoryKvRow(
                                  label: tr.rejectCodeLabel,
                                  value: command.rejectCode!,
                                ),
                              if (command.targetAlarmEventUid != null)
                                HistoryKvRow(
                                  label: tr.targetAlarmEventUidLabel,
                                  value: command.targetAlarmEventUid!,
                                ),
                              HistoryKvRow(
                                label: tr.actualStateChangedLabel,
                                value: command.actualStateChanged
                                    ? tr.yes
                                    : tr.no,
                              ),
                              HistoryKvRow(
                                label: tr.commandIdLabel,
                                value: command.commandId,
                              ),
                            ],
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
        ),
      ),
    );
  }
}
