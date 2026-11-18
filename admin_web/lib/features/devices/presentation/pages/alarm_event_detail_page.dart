import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/router/app_routes.dart';
import '../../../../core/widgets/owner_visual.dart';
import '../history_format.dart';
import '../viewmodels/device_history_view_models.dart';
import '../widgets/device_status_pills.dart';
import '../widgets/history_kv_row.dart';
import '../widgets/info_notice.dart';

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
    final key = AlarmEventDetailArgs(
      deviceId: deviceId,
      alarmEventId: alarmEventId,
    );
    final state = ref.watch(alarmEventDetailViewModelProvider(key));
    final vm = ref.read(alarmEventDetailViewModelProvider(key).notifier);
    final event = state.event;
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
          onPressed: () =>
              context.go(AppRoutes.ownerDeviceHistoryPath(deviceId)),
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
          child: state.isLoading && event == null
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
                  padding: OwnerLayout.pagePadding(width),
                  children: [
                    OwnerPageHeader(
                      title: tr.alarmEventDetailTitle,
                      subtitle: OwnerVisualCopy.eventDetailSubtitle,
                    ),
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
                    _DetailCard(
                      title: tr.alarmEventLabel,
                      trailing: DeviceStatusLabels.eventClass(context, event),
                      child: HistoryKvGrid(
                        rows: [
                          HistoryKvRow(
                            label: tr.deviceIdLabel,
                            value: event.deviceId,
                          ),
                          HistoryKvRow(
                            label: tr.sensorLabel,
                            value: event.sensorKeySnapshot,
                          ),
                          HistoryKvRow(
                            label: tr.statusLabel,
                            value: event.status.code,
                          ),
                          HistoryKvRow(
                            label: tr.receivedAtLabel,
                            value: formatHistoryTime(event.receivedAt),
                          ),
                          HistoryKvRow(
                            label: tr.occurredAtLabel,
                            value: occurrenceText(tr, event),
                          ),
                          HistoryKvRow(
                            label: tr.eventUidLabel,
                            value: event.eventUid,
                          ),
                          HistoryKvRow(
                            label: tr.alarmEventIdLabel,
                            value: event.alarmEventId,
                          ),
                          HistoryKvRow(
                            label: tr.wasBufferedLabel,
                            value: event.wasBuffered ? tr.yes : tr.no,
                          ),
                          if (event.mutedAt != null)
                            HistoryKvRow(
                              label: tr.mutedAtLabel,
                              value: formatHistoryTime(event.mutedAt),
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

class _DetailCard extends StatelessWidget {
  const _DetailCard({required this.title, required this.child, this.trailing});

  final String title;
  final Widget child;
  final Widget? trailing;

  @override
  Widget build(BuildContext context) {
    return Container(
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
          Row(
            children: [
              Expanded(
                child: Text(
                  title,
                  style: Theme.of(context).textTheme.titleLarge,
                ),
              ),
              if (trailing != null) trailing!,
            ],
          ),
          const SizedBox(height: DesignTokens.spaceLg),
          child,
        ],
      ),
    );
  }
}
