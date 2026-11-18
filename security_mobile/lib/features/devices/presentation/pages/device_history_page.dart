import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/domain/device_status_codes.dart';
import '../../../../core/router/app_routes.dart';
import '../../domain/entities/alarm_event_entity.dart';
import '../../domain/entities/device_command_entity.dart';
import '../viewmodels/device_history_view_models.dart';

class DeviceHistoryPage extends ConsumerStatefulWidget {
  const DeviceHistoryPage({
    super.key,
    required this.deviceId,
    this.initialTab = 'events',
  });

  final String deviceId;
  final String initialTab;

  @override
  ConsumerState<DeviceHistoryPage> createState() => _DeviceHistoryPageState();
}

class _DeviceHistoryPageState extends ConsumerState<DeviceHistoryPage>
    with SingleTickerProviderStateMixin {
  late final TabController _tabs;

  @override
  void initState() {
    super.initState();
    _tabs = TabController(
      length: 2,
      vsync: this,
      initialIndex: widget.initialTab == 'commands' ? 1 : 0,
    );
  }

  @override
  void dispose() {
    _tabs.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final tr = context.tr;
    final state =
        ref.watch(deviceHistoryListViewModelProvider(widget.deviceId));
    final vm =
        ref.read(deviceHistoryListViewModelProvider(widget.deviceId).notifier);

    return Scaffold(
      backgroundColor: DesignTokens.background,
      appBar: AppBar(
        title: Text(tr.deviceHistoryTitle),
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () =>
              context.go(AppRoutes.deviceDetailPath(widget.deviceId)),
        ),
        actions: [
          IconButton(
            tooltip: tr.refresh,
            onPressed: vm.load,
            icon: const Icon(Icons.refresh),
          ),
        ],
        bottom: TabBar(
          controller: _tabs,
          tabs: [
            Tab(text: tr.alarmEventsTab),
            Tab(text: tr.commandsHistoryTab),
          ],
        ),
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(DesignTokens.spaceMd),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                TextField(
                  onChanged: vm.setQuery,
                  decoration: InputDecoration(
                    hintText: tr.searchHistoryHint,
                    prefixIcon: const Icon(Icons.search),
                    border: OutlineInputBorder(
                      borderRadius:
                          BorderRadius.circular(DesignTokens.radiusMd),
                    ),
                  ),
                ),
                const SizedBox(height: DesignTokens.spaceSm),
                Wrap(
                  spacing: DesignTokens.spaceSm,
                  children: [
                    FilterChip(
                      label: Text(tr.bufferedLabel),
                      selected: state.bufferedOnly,
                      onSelected: vm.setBufferedOnly,
                    ),
                    FilterChip(
                      label: const Text('PENDING'),
                      selected: state.commandStatusFilter == 'PENDING',
                      onSelected: (on) => vm.setCommandStatusFilter(
                        on ? 'PENDING' : null,
                      ),
                    ),
                    FilterChip(
                      label: const Text('TIMED_OUT'),
                      selected: state.commandStatusFilter == 'TIMED_OUT',
                      onSelected: (on) => vm.setCommandStatusFilter(
                        on ? 'TIMED_OUT' : null,
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
          Expanded(
            child: state.isLoading &&
                    state.events.isEmpty &&
                    state.commands.isEmpty
                ? const Center(child: CircularProgressIndicator())
                : state.error != null &&
                        state.events.isEmpty &&
                        state.commands.isEmpty
                    ? MyEmptyState(
                        type: MyEmptyType.error,
                        title: tr.error,
                        subtitle: state.error!.message,
                        primaryActionLabel: tr.retry,
                        onPrimaryAction: vm.load,
                      )
                    : TabBarView(
                        controller: _tabs,
                        children: [
                          _EventsList(
                            deviceId: widget.deviceId,
                            events: state.filteredEvents,
                          ),
                          _CommandsList(
                            deviceId: widget.deviceId,
                            commands: state.filteredCommands,
                          ),
                        ],
                      ),
          ),
        ],
      ),
    );
  }
}

class _EventsList extends StatelessWidget {
  const _EventsList({required this.deviceId, required this.events});

  final String deviceId;
  final List<AlarmEventEntity> events;

  @override
  Widget build(BuildContext context) {
    final tr = context.tr;
    if (events.isEmpty) {
      return MyEmptyState(
        type: MyEmptyType.empty,
        title: tr.noAlarmEventsTitle,
        subtitle: tr.noAlarmEventsSubtitle,
      );
    }
    return ListView.separated(
      padding: const EdgeInsets.all(DesignTokens.spaceMd),
      itemCount: events.length,
      separatorBuilder: (_, __) =>
          const SizedBox(height: DesignTokens.spaceSm),
      itemBuilder: (context, index) {
        final e = events[index];
        return ListTile(
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(DesignTokens.radiusMd),
            side: const BorderSide(color: DesignTokens.border),
          ),
          title: Text(tr.alarmEventLabel),
          subtitle: Text(
            '${tr.receivedAtLabel}: ${e.receivedAt}\n'
            '${e.status.code}'
            '${e.showBufferedNotice ? ' · ${tr.bufferedLabel}' : ''}',
          ),
          trailing: const Icon(Icons.chevron_left),
          onTap: () => context.go(
            AppRoutes.deviceAlarmEventDetailPath(deviceId, e.alarmEventId),
          ),
        );
      },
    );
  }
}

class _CommandsList extends StatelessWidget {
  const _CommandsList({required this.deviceId, required this.commands});

  final String deviceId;
  final List<DeviceCommandEntity> commands;

  @override
  Widget build(BuildContext context) {
    final tr = context.tr;
    if (commands.isEmpty) {
      return MyEmptyState(
        type: MyEmptyType.empty,
        title: tr.noCommandsTitle,
        subtitle: tr.noCommandsSubtitle,
      );
    }
    return ListView.separated(
      padding: const EdgeInsets.all(DesignTokens.spaceMd),
      itemCount: commands.length,
      separatorBuilder: (_, __) =>
          const SizedBox(height: DesignTokens.spaceSm),
      itemBuilder: (context, index) {
        final c = commands[index];
        final isTest = c.type == CommandType.testSiren ||
            c.type == CommandType.stopTest;
        return ListTile(
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(DesignTokens.radiusMd),
            side: const BorderSide(color: DesignTokens.border),
          ),
          title: Text(
            isTest ? '${tr.sirenTestCommand} · ${c.type.code}' : c.type.code,
          ),
          subtitle: Text('${c.status.code} · ${c.createdAt}'),
          trailing: const Icon(Icons.chevron_left),
          onTap: () => context.go(
            AppRoutes.deviceCommandDetailPath(deviceId, c.commandId),
          ),
        );
      },
    );
  }
}
