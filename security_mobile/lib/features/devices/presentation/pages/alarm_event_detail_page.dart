import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/router/app_routes.dart';
import '../viewmodels/device_history_view_models.dart';
import '../widgets/info_notice.dart';
import '../widgets/kv_row.dart';

class AlarmEventDetailPage extends ConsumerWidget {
  const AlarmEventDetailPage({
    super.key,
    required this.deviceId,
    required this.alarmEventId,
  });

  final String deviceId;
  final String alarmEventId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final tr = context.tr;
    final key = (deviceId: deviceId, alarmEventId: alarmEventId);
    final state = ref.watch(alarmEventDetailViewModelProvider(key));
    final vm = ref.read(alarmEventDetailViewModelProvider(key).notifier);
    final theme = Theme.of(context);
    final event = state.event;

    return Scaffold(
      backgroundColor: DesignTokens.background,
      appBar: AppBar(
        title: Text(tr.alarmEventDetailTitle),
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => context.go(AppRoutes.deviceHistoryPath(deviceId)),
        ),
        actions: [
          IconButton(
            tooltip: tr.refresh,
            onPressed: vm.load,
            icon: const Icon(Icons.refresh),
          ),
        ],
      ),
      body: state.isLoading && event == null
          ? const Center(child: CircularProgressIndicator())
          : state.error != null && event == null
              ? MyEmptyState(
                  type: MyEmptyType.error,
                  title: tr.error,
                  subtitle: state.error!.message,
                  primaryActionLabel: tr.retry,
                  onPrimaryAction: vm.load,
                )
              : event == null
                  ? MyEmptyState(
                      type: MyEmptyType.empty,
                      title: tr.noAlarmEventsTitle,
                      subtitle: tr.noAlarmEventsSubtitle,
                    )
                  : ListView(
                      padding: const EdgeInsets.all(DesignTokens.spaceLg),
                      children: [
                        if (event.showBufferedNotice)
                          InfoNotice(
                            text: tr.bufferedNotice,
                            tone: InfoNoticeTone.warning,
                          ),
                        if (!event.hasTrustedOccurrence)
                          InfoNotice(
                            text: tr.occurrenceUntrusted,
                            tone: InfoNoticeTone.warning,
                          ),
                        KvRow(tr.deviceIdLabel, event.deviceId),
                        KvRow(tr.sensorLabel, event.sensorKeySnapshot),
                        KvRow(tr.statusLabel, event.status.code),
                        KvRow(tr.receivedAtLabel, '${event.receivedAt}'),
                        KvRow(
                          tr.occurredAtLabel,
                          event.hasTrustedOccurrence
                              ? '${event.occurredDeviceAt}'
                              : tr.occurrenceUntrustedShort,
                        ),
                        KvRow(tr.eventUidLabel, event.eventUid),
                        KvRow(tr.alarmEventIdLabel, event.alarmEventId),
                        KvRow(
                          tr.wasBufferedLabel,
                          event.wasBuffered ? tr.yes : tr.no,
                        ),
                        if (event.mutedAt != null)
                          KvRow(tr.mutedAtLabel, '${event.mutedAt}'),
                        const SizedBox(height: DesignTokens.spaceLg),
                        Text(
                          tr.notificationLinkLater,
                          style: theme.textTheme.bodySmall?.copyWith(
                            color: DesignTokens.muted,
                          ),
                        ),
                      ],
                    ),
    );
  }
}
