import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/router/app_routes.dart';
import '../../domain/audit_log_query.dart';
import '../../domain/entities/audit_log_entity.dart';
import '../admin_copy.dart';
import '../audit_labels.dart';
import '../viewmodels/audit_logs_view_model.dart';
import '../widgets/admin_widgets.dart';

class AuditLogsPage extends ConsumerStatefulWidget {
  const AuditLogsPage({super.key});

  @override
  ConsumerState<AuditLogsPage> createState() => _AuditLogsPageState();
}

class _AuditLogsPageState extends ConsumerState<AuditLogsPage> {
  final _q = TextEditingController();
  final _action = TextEditingController();
  final _targetType = TextEditingController();
  final _actor = TextEditingController();
  final _device = TextEditingController();
  final _target = TextEditingController();
  final _result = TextEditingController();
  final _from = TextEditingController();
  final _to = TextEditingController();
  String? _filterError;

  @override
  void dispose() {
    _q.dispose();
    _action.dispose();
    _targetType.dispose();
    _actor.dispose();
    _device.dispose();
    _target.dispose();
    _result.dispose();
    _from.dispose();
    _to.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(auditLogsViewModelProvider);
    final vm = ref.read(auditLogsViewModelProvider.notifier);
    ref.listen(auditLogsViewModelProvider, (prev, next) {
      final error = next.error;
      if (error == null || error == prev?.error) return;
      if (error.displayType != ErrorDisplayType.inline) {
        showErrorSnackBar(context, error);
      }
    });

    return AdminPageBody(
      title: AdminCopy.auditLogs,
      subtitle: AdminCopy.auditSubtitle,
      action: OutlinedButton(
        style: AdminButtonStyles.outline,
        onPressed: state.loading ? null : vm.load,
        child: const Text(AdminCopy.refresh),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const AdminNotice(text: AdminCopy.auditNotice),
          AdminPanel(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                AdminSearchField(
                  controller: _q,
                  hint: AdminCopy.auditSearchHint,
                  onSubmitted: (_) => _apply(vm),
                ),
                const SizedBox(height: 8),
                ExpansionTile(
                  title: const Text(AdminCopy.auditFilters),
                  children: [
                    AdminField(
                      label: AdminCopy.auditAction,
                      controller: _action,
                    ),
                    AdminField(
                      label: AdminCopy.targetType,
                      controller: _targetType,
                    ),
                    AdminField(label: AdminCopy.actor, controller: _actor),
                    AdminField(label: AdminCopy.deviceId, controller: _device),
                    AdminField(label: AdminCopy.targetId, controller: _target),
                    AdminField(label: AdminCopy.result, controller: _result),
                    AdminField(label: AdminCopy.from, controller: _from),
                    AdminField(label: AdminCopy.to, controller: _to),
                  ],
                ),
                if (_filterError != null)
                  AdminNotice(text: _filterError!, danger: true),
                Align(
                  alignment: AlignmentDirectional.centerStart,
                  child: FilledButton(
                    style: AdminButtonStyles.primary,
                    onPressed: state.loading ? null : () => _apply(vm),
                    child: const Text(AdminCopy.applyFilters),
                  ),
                ),
                const SizedBox(height: 17),
                _AuditBody(state: state, onRetry: vm.load),
              ],
            ),
          ),
          const SizedBox(height: 16),
          AdminPager(
            page: state.page,
            total: state.totalCount,
            enabled: !state.loading,
            hasPrevious: state.hasPrevious,
            hasNext: state.hasNext,
            onPrevious: () => vm.openPage(state.page - 1),
            onNext: () => vm.openPage(state.page + 1),
          ),
        ],
      ),
    );
  }

  void _apply(AuditLogsViewModel vm) {
    final actorError = AuditLogQueryRules.optionalGuid(_actor.text);
    final deviceError = AuditLogQueryRules.optionalGuid(_device.text);
    final targetError = AuditLogQueryRules.optionalGuid(_target.text);
    if (actorError != null || deviceError != null || targetError != null) {
      setState(() => _filterError = AdminCopy.fieldMessage('guid'));
      return;
    }
    final fromText = _from.text.trim();
    final toText = _to.text.trim();
    final from = AuditLogQueryRules.optionalInstant(fromText);
    final to = AuditLogQueryRules.optionalInstant(toText);
    if ((fromText.isNotEmpty && from == null) ||
        (toText.isNotEmpty && to == null)) {
      setState(() => _filterError = AdminCopy.fieldMessage('instant'));
      return;
    }
    setState(() => _filterError = null);
    vm.apply(
      AuditLogQuery(
        q: _empty(_q.text),
        action: _empty(_action.text),
        targetType: _empty(_targetType.text),
        actorUserId: _empty(_actor.text),
        deviceId: _empty(_device.text),
        targetId: _empty(_target.text),
        result: _empty(_result.text),
        from: from,
        to: to,
        pageSize: ref.read(auditLogsViewModelProvider).pageSize,
      ),
    );
  }

  String? _empty(String value) {
    final text = value.trim();
    return text.isEmpty ? null : text;
  }
}

class _AuditBody extends ConsumerWidget {
  const _AuditBody({required this.state, required this.onRetry});

  final AuditLogsState state;
  final Future<void> Function() onRetry;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    if (state.loading && state.items.isEmpty) {
      return const Padding(
        padding: EdgeInsets.all(32),
        child: Center(child: CircularProgressIndicator()),
      );
    }
    if (state.error != null && state.items.isEmpty) {
      return MyEmptyState(
        type: MyEmptyType.error,
        title: context.tr.error,
        subtitle: state.error!.message,
        primaryActionLabel: context.tr.retry,
        onPrimaryAction: onRetry,
      );
    }
    if (state.items.isEmpty) {
      return const MyEmptyState(
        type: MyEmptyType.empty,
        title: AdminCopy.emptyAudit,
        subtitle: AdminCopy.auditSubtitle,
      );
    }
    return AdminTable(
      headers: const [
        AdminCopy.logId,
        AdminCopy.auditAction,
        AdminCopy.targetType,
        AdminCopy.targetId,
        AdminCopy.deviceId,
        AdminCopy.result,
        AdminCopy.occurredAt,
        '',
      ],
      rows: [
        for (final log in state.items)
          [
            Text(log.id),
            Text('${auditActionLabel(log.action)} (${log.action})'),
            Text(log.targetType),
            Text(log.targetId ?? '—'),
            Text(log.deviceId ?? '—'),
            Text('${auditResultLabel(log.result)} (${log.result})'),
            Text(formatAdminTime(log.occurredAt)),
            AdminTextLink(
              label: AdminCopy.detailsLink,
              onPressed: () {
                ref.read(selectedAuditLogProvider.notifier).select(log);
                context.go(AppRoutes.adminAuditLogPath(log.id));
              },
            ),
          ],
      ],
    );
  }
}

class AuditLogDetailPage extends ConsumerWidget {
  const AuditLogDetailPage({super.key, required this.logId});

  final String logId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final selected = ref.watch(selectedAuditLogProvider);
    final log = selected != null && selected.id == logId ? selected : null;
    return AdminPageBody(
      title: AdminCopy.auditDetail,
      subtitle: AdminCopy.auditSubtitle,
      action: OutlinedButton(
        style: AdminButtonStyles.outline,
        onPressed: () => context.go(AppRoutes.adminAuditLogs),
        child: const Text('← ${AdminCopy.back}'),
      ),
      child: log == null
          ? const MyEmptyState(
              type: MyEmptyType.empty,
              title: AdminCopy.auditMissing,
              subtitle: AdminCopy.auditMissingHint,
            )
          : _AuditDetail(log: log),
    );
  }
}

class _AuditDetail extends StatelessWidget {
  const _AuditDetail({required this.log});

  final AuditLogEntity log;

  @override
  Widget build(BuildContext context) {
    return AdminPanel(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          AdminSectionTitle(
            title: log.id,
            action: AdminPill(auditResultLabel(log.result), AdminPillTone.blue),
          ),
          AdminKvGrid(
            entries: [
              (AdminCopy.logId, log.id),
              (
                AdminCopy.auditAction,
                '${auditActionLabel(log.action)} (${log.action})',
              ),
              (AdminCopy.targetType, log.targetType),
              (AdminCopy.targetId, log.targetId ?? '—'),
              (AdminCopy.deviceId, log.deviceId ?? '—'),
              (AdminCopy.actor, log.actorUserId ?? '—'),
              (
                AdminCopy.result,
                '${auditResultLabel(log.result)} (${log.result})',
              ),
              (AdminCopy.occurredAt, formatAdminTime(log.occurredAt)),
              (AdminCopy.details, log.details ?? '—'),
            ],
          ),
        ],
      ),
    );
  }
}
