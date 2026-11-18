import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/router/app_routes.dart';
import '../../../../core/widgets/owner_visual.dart';
import '../../domain/entities/alarm_event_entity.dart';
import '../../domain/entities/device_command_entity.dart';
import '../history_format.dart';
import '../viewmodels/device_history_view_models.dart';
import '../widgets/device_status_pills.dart';

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
  bool _syncingTab = false;

  @override
  void initState() {
    super.initState();
    _tabs = TabController(
      length: 2,
      vsync: this,
      initialIndex: widget.initialTab == 'commands' ? 1 : 0,
    );
    _tabs.addListener(_syncTabRoute);
  }

  @override
  void didUpdateWidget(covariant DeviceHistoryPage oldWidget) {
    super.didUpdateWidget(oldWidget);
    final index = widget.initialTab == 'commands' ? 1 : 0;
    if (_tabs.index == index) return;
    _syncingTab = true;
    _tabs.index = index;
    _syncingTab = false;
  }

  void _syncTabRoute() {
    if (_syncingTab || _tabs.indexIsChanging || !mounted) return;
    final tab = _tabs.index == 1 ? 'commands' : 'events';
    final current =
        GoRouterState.of(context).uri.queryParameters['tab'] ?? 'events';
    if (current == tab) return;
    context.go(AppRoutes.ownerDeviceHistoryPath(widget.deviceId, tab: tab));
  }

  @override
  void dispose() {
    _tabs.removeListener(_syncTabRoute);
    _tabs.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final tr = context.tr;
    final state = ref.watch(
      deviceHistoryListViewModelProvider(widget.deviceId),
    );
    final vm = ref.read(
      deviceHistoryListViewModelProvider(widget.deviceId).notifier,
    );
    final width = MediaQuery.sizeOf(context).width;

    ref.listen(deviceHistoryListViewModelProvider(widget.deviceId), (
      prev,
      next,
    ) {
      final error = next.error;
      if (error == null || error == prev?.error) return;
      if (next.events.isEmpty && next.commands.isEmpty) return;
      showErrorSnackBar(context, error);
    });

    final blocked =
        state.error != null && state.events.isEmpty && state.commands.isEmpty;

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
              context.go(AppRoutes.ownerDeviceDetailPath(widget.deviceId)),
        ),
        actions: [
          IconButton(
            tooltip: tr.refresh,
            onPressed: state.isLoading ? null : vm.load,
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
      body: Align(
        alignment: Alignment.topCenter,
        child: ConstrainedBox(
          constraints: const BoxConstraints(
            maxWidth: OwnerLayout.contentMaxWidth,
          ),
          child: Column(
            children: [
              Padding(
                padding: OwnerLayout.pagePadding(width).copyWith(bottom: 0),
                child: OwnerPageHeader(
                  title: tr.deviceHistoryTitle,
                  subtitle: OwnerVisualCopy.historySubtitle,
                ),
              ),
              Padding(
                padding: const EdgeInsets.fromLTRB(
                  DesignTokens.spaceLg,
                  DesignTokens.spaceLg,
                  DesignTokens.spaceLg,
                  DesignTokens.spaceSm,
                ),
                child: _Filters(state: state, vm: vm),
              ),
              Expanded(
                child:
                    state.isLoading &&
                        state.events.isEmpty &&
                        state.commands.isEmpty
                    ? const Center(child: CircularProgressIndicator())
                    : blocked
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
                          _EventsPane(
                            deviceId: widget.deviceId,
                            state: state,
                            onPage: vm.setEventsPage,
                          ),
                          _CommandsPane(
                            deviceId: widget.deviceId,
                            state: state,
                            onPage: vm.setCommandsPage,
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

class _Filters extends StatelessWidget {
  const _Filters({required this.state, required this.vm});

  final DeviceHistoryListState state;
  final DeviceHistoryListViewModel vm;

  @override
  Widget build(BuildContext context) {
    final tr = context.tr;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        TextField(
          onChanged: vm.setQuery,
          decoration: InputDecoration(
            hintText: tr.searchHistoryHint,
            prefixIcon: const Icon(Icons.search),
          ),
        ),
        const SizedBox(height: DesignTokens.spaceSm),
        Wrap(
          spacing: DesignTokens.spaceSm,
          runSpacing: DesignTokens.spaceSm,
          children: [
            FilterChip(
              label: Text(tr.bufferedLabel),
              selected: state.bufferedOnly,
              onSelected: vm.setBufferedOnly,
            ),
            FilterChip(
              label: const Text('PENDING'),
              selected: state.commandStatusFilter == 'PENDING',
              onSelected: (on) =>
                  vm.setCommandStatusFilter(on ? 'PENDING' : null),
            ),
            FilterChip(
              label: const Text('TIMED_OUT'),
              selected: state.commandStatusFilter == 'TIMED_OUT',
              onSelected: (on) =>
                  vm.setCommandStatusFilter(on ? 'TIMED_OUT' : null),
            ),
          ],
        ),
      ],
    );
  }
}

class _EventsPane extends StatelessWidget {
  const _EventsPane({
    required this.deviceId,
    required this.state,
    required this.onPage,
  });

  final String deviceId;
  final DeviceHistoryListState state;
  final ValueChanged<int> onPage;

  @override
  Widget build(BuildContext context) {
    final tr = context.tr;
    final page = state.eventsWindow;
    if (state.filteredEvents.isEmpty) {
      return MyEmptyState(
        type: state.query.isEmpty && !state.bufferedOnly
            ? MyEmptyType.empty
            : MyEmptyType.search,
        title: state.query.isEmpty && !state.bufferedOnly
            ? tr.noAlarmEventsTitle
            : tr.noResults,
        subtitle: tr.noAlarmEventsSubtitle,
      );
    }
    final wide = MediaQuery.sizeOf(context).width >= 900;
    return Column(
      children: [
        Expanded(
          child: wide
              ? _EventsTable(deviceId: deviceId, events: page.items)
              : _EventsCards(deviceId: deviceId, events: page.items),
        ),
        _Pager(
          pageIndex: page.pageIndex,
          pageCount: page.pageCount,
          onPage: onPage,
        ),
      ],
    );
  }
}

class _CommandsPane extends StatelessWidget {
  const _CommandsPane({
    required this.deviceId,
    required this.state,
    required this.onPage,
  });

  final String deviceId;
  final DeviceHistoryListState state;
  final ValueChanged<int> onPage;

  @override
  Widget build(BuildContext context) {
    final tr = context.tr;
    final page = state.commandsWindow;
    if (state.filteredCommands.isEmpty) {
      return MyEmptyState(
        type: state.query.isEmpty && state.commandStatusFilter == null
            ? MyEmptyType.empty
            : MyEmptyType.search,
        title: state.query.isEmpty && state.commandStatusFilter == null
            ? tr.noCommandsTitle
            : tr.noResults,
        subtitle: tr.noCommandsSubtitle,
      );
    }
    final wide = MediaQuery.sizeOf(context).width >= 900;
    return Column(
      children: [
        Expanded(
          child: wide
              ? _CommandsTable(deviceId: deviceId, commands: page.items)
              : _CommandsCards(deviceId: deviceId, commands: page.items),
        ),
        _Pager(
          pageIndex: page.pageIndex,
          pageCount: page.pageCount,
          onPage: onPage,
        ),
      ],
    );
  }
}

class _EventsTable extends StatelessWidget {
  const _EventsTable({required this.deviceId, required this.events});

  final String deviceId;
  final List<AlarmEventEntity> events;

  @override
  Widget build(BuildContext context) {
    final tr = context.tr;
    return _Panel(
      child: ListView(
        padding: const EdgeInsets.all(DesignTokens.spaceMd),
        children: [
          _HeaderRow(
            cells: [
              tr.alarmEventIdLabel,
              tr.deviceIdLabel,
              tr.alarmEventLabel,
              tr.receivedAtLabel,
              tr.statusLabel,
              '',
            ],
          ),
          for (final event in events)
            _DataRow(
              onTap: () => context.go(
                AppRoutes.ownerDeviceAlarmEventPath(
                  deviceId,
                  event.alarmEventId,
                ),
              ),
              cells: [
                Text(event.alarmEventId, style: _cellStyle(context)),
                Text(event.deviceId, style: _cellStyle(context)),
                Text(tr.alarmEventLabel, style: _cellStyle(context)),
                Text(
                  formatHistoryTime(event.receivedAt),
                  style: _cellStyle(context),
                ),
                DeviceStatusLabels.eventClass(context, event),
                Text(OwnerVisualCopy.details, style: _linkStyle(context)),
              ],
            ),
        ],
      ),
    );
  }
}

class _EventsCards extends StatelessWidget {
  const _EventsCards({required this.deviceId, required this.events});

  final String deviceId;
  final List<AlarmEventEntity> events;

  @override
  Widget build(BuildContext context) {
    final tr = context.tr;
    return ListView.separated(
      padding: const EdgeInsets.all(DesignTokens.spaceLg),
      itemCount: events.length,
      separatorBuilder: (_, __) => const SizedBox(height: DesignTokens.spaceSm),
      itemBuilder: (context, index) {
        final event = events[index];
        return _RecordCard(
          title: tr.alarmEventLabel,
          subtitle:
              '${formatHistoryTime(event.receivedAt)}\n${_eventStatus(tr, event)}',
          onTap: () => context.go(
            AppRoutes.ownerDeviceAlarmEventPath(deviceId, event.alarmEventId),
          ),
        );
      },
    );
  }
}

class _CommandsTable extends StatelessWidget {
  const _CommandsTable({required this.deviceId, required this.commands});

  final String deviceId;
  final List<DeviceCommandEntity> commands;

  @override
  Widget build(BuildContext context) {
    final tr = context.tr;
    return _Panel(
      child: ListView(
        padding: const EdgeInsets.all(DesignTokens.spaceMd),
        children: [
          _HeaderRow(
            cells: [
              tr.commandIdLabel,
              tr.deviceIdLabel,
              tr.commandTypeLabel,
              tr.createdAtLabel,
              tr.statusLabel,
              '',
            ],
          ),
          for (final command in commands)
            _DataRow(
              onTap: () => context.go(
                AppRoutes.ownerDeviceCommandPath(deviceId, command.commandId),
              ),
              cells: [
                Text(command.commandId, style: _cellStyle(context)),
                Text(command.deviceId, style: _cellStyle(context)),
                Text(
                  commandHistoryTitle(tr, command),
                  style: _cellStyle(context),
                ),
                Text(
                  formatHistoryTime(command.createdAt),
                  style: _cellStyle(context),
                ),
                DeviceStatusLabels.commandStatus(command.status),
                Text(OwnerVisualCopy.details, style: _linkStyle(context)),
              ],
            ),
        ],
      ),
    );
  }
}

class _CommandsCards extends StatelessWidget {
  const _CommandsCards({required this.deviceId, required this.commands});

  final String deviceId;
  final List<DeviceCommandEntity> commands;

  @override
  Widget build(BuildContext context) {
    final tr = context.tr;
    return ListView.separated(
      padding: const EdgeInsets.all(DesignTokens.spaceLg),
      itemCount: commands.length,
      separatorBuilder: (_, __) => const SizedBox(height: DesignTokens.spaceSm),
      itemBuilder: (context, index) {
        final command = commands[index];
        return _RecordCard(
          title: commandHistoryTitle(tr, command),
          subtitle:
              '${command.status.code} · ${formatHistoryTime(command.createdAt)}',
          onTap: () => context.go(
            AppRoutes.ownerDeviceCommandPath(deviceId, command.commandId),
          ),
        );
      },
    );
  }
}

String _eventStatus(AppLocalizations tr, AlarmEventEntity event) {
  final buffered = event.showBufferedNotice ? ' · ${tr.bufferedLabel}' : '';
  final untrusted = event.hasTrustedOccurrence
      ? ''
      : ' · ${tr.occurrenceUntrustedShort}';
  return '${event.status.code}$buffered$untrusted';
}

class _Panel extends StatelessWidget {
  const _Panel({required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(
        DesignTokens.spaceLg,
        0,
        DesignTokens.spaceLg,
        DesignTokens.spaceSm,
      ),
      child: DecoratedBox(
        decoration: BoxDecoration(
          color: DesignTokens.card,
          borderRadius: BorderRadius.circular(DesignTokens.radiusLg),
          border: Border.all(color: DesignTokens.border),
          boxShadow: DesignTokens.cardShadow,
        ),
        child: child,
      ),
    );
  }
}

class _HeaderRow extends StatelessWidget {
  const _HeaderRow({required this.cells});

  final List<String> cells;

  @override
  Widget build(BuildContext context) {
    final style = Theme.of(context).textTheme.labelSmall?.copyWith(
      color: DesignTokens.muted,
      fontWeight: FontWeight.w700,
    );
    return ColoredBox(
      color: AppColors.lightSurfaceVariant,
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 13, vertical: 13),
        child: Row(
          children: [
            for (final cell in cells)
              Expanded(
                child: Text(
                  cell,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: style,
                ),
              ),
          ],
        ),
      ),
    );
  }
}

TextStyle? _cellStyle(BuildContext context) =>
    Theme.of(context).textTheme.bodySmall?.copyWith(color: DesignTokens.ink);

TextStyle? _linkStyle(BuildContext context) => Theme.of(
  context,
).textTheme.labelLarge?.copyWith(color: DesignTokens.primary);

class _DataRow extends StatelessWidget {
  const _DataRow({required this.cells, required this.onTap});

  final List<Widget> cells;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      child: DecoratedBox(
        decoration: const BoxDecoration(
          border: Border(bottom: BorderSide(color: AppColors.lightDivider)),
        ),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 13, vertical: 14),
          child: Row(
            children: [
              for (final cell in cells)
                Expanded(
                  child: DefaultTextStyle.merge(
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                    child: Align(
                      alignment: AlignmentDirectional.centerStart,
                      child: cell,
                    ),
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }
}

class _RecordCard extends StatelessWidget {
  const _RecordCard({
    required this.title,
    required this.subtitle,
    required this.onTap,
  });

  final String title;
  final String subtitle;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: DesignTokens.card,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(DesignTokens.radiusMd),
        side: const BorderSide(color: DesignTokens.border),
      ),
      child: ListTile(
        title: Text(title),
        subtitle: Text(subtitle),
        trailing: const Icon(Icons.chevron_left),
        onTap: onTap,
      ),
    );
  }
}

class _Pager extends StatelessWidget {
  const _Pager({
    required this.pageIndex,
    required this.pageCount,
    required this.onPage,
  });

  final int pageIndex;
  final int pageCount;
  final ValueChanged<int> onPage;

  @override
  Widget build(BuildContext context) {
    final tr = context.tr;
    return Padding(
      padding: const EdgeInsets.fromLTRB(
        DesignTokens.spaceLg,
        0,
        DesignTokens.spaceLg,
        DesignTokens.spaceLg,
      ),
      child: Wrap(
        spacing: 8,
        runSpacing: 8,
        crossAxisAlignment: WrapCrossAlignment.center,
        children: [
          TextButton(
            onPressed: pageIndex > 0 ? () => onPage(pageIndex - 1) : null,
            child: Text(tr.back),
          ),
          Text('${pageIndex + 1} / $pageCount'),
          TextButton(
            onPressed: pageIndex + 1 < pageCount
                ? () => onPage(pageIndex + 1)
                : null,
            child: Text(tr.next),
          ),
        ],
      ),
    );
  }
}
