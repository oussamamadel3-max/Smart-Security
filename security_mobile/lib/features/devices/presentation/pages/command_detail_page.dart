import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/router/app_routes.dart';
import '../viewmodels/device_history_view_models.dart';
import '../widgets/info_notice.dart';
import '../widgets/kv_row.dart';

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
    final key = (deviceId: deviceId, commandId: commandId);
    final state = ref.watch(commandDetailViewModelProvider(key));
    final vm = ref.read(commandDetailViewModelProvider(key).notifier);
    final command = state.command;

    return Scaffold(
      backgroundColor: DesignTokens.background,
      appBar: AppBar(
        title: Text(tr.commandDetailTitle),
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => context.go(
            AppRoutes.deviceHistoryPath(deviceId, tab: 'commands'),
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
      body: state.isLoading && command == null
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
                      padding: const EdgeInsets.all(DesignTokens.spaceLg),
                      children: [
                        if (command.status == CommandStatus.timedOut)
                          InfoNotice(
                            text: tr.timedOutUnknownNotice,
                            tone: InfoNoticeTone.warning,
                          ),
                        KvRow(tr.commandTypeLabel, command.type.code),
                        KvRow(tr.statusLabel, command.status.code),
                        KvRow(tr.createdAtLabel, '${command.createdAt}'),
                        KvRow(tr.expiresAtLabel, '${command.expiresAt}'),
                        if (command.rejectCode != null)
                          KvRow(tr.rejectCodeLabel, command.rejectCode!),
                        if (command.targetAlarmEventUid != null)
                          KvRow(
                            tr.targetAlarmEventUidLabel,
                            command.targetAlarmEventUid!,
                          ),
                        KvRow(
                          tr.actualStateChangedLabel,
                          command.actualStateChanged ? tr.yes : tr.no,
                        ),
                        KvRow(tr.commandIdLabel, command.commandId),
                      ],
                    ),
    );
  }
}
