import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/router/app_routes.dart';
import '../../domain/audit_log_query.dart';
import '../admin_alert_facts.dart';
import '../admin_copy.dart';
import '../viewmodels/admin_alerts_view_model.dart';
import '../widgets/admin_alert_outcome_panel.dart';
import '../widgets/admin_widgets.dart';

class AdminAlertsPage extends ConsumerStatefulWidget {
  const AdminAlertsPage({super.key});

  @override
  ConsumerState<AdminAlertsPage> createState() => _AdminAlertsPageState();
}

class _AdminAlertsPageState extends ConsumerState<AdminAlertsPage> {
  final _recipient = TextEditingController();
  String? _filterError;

  @override
  void dispose() {
    _recipient.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(adminAlertsViewModelProvider);
    final vm = ref.read(adminAlertsViewModelProvider.notifier);
    ref.listen(adminAlertsViewModelProvider, (prev, next) {
      final error = next.error;
      if (error == null || error == prev?.error) return;
      if (error.displayType != ErrorDisplayType.inline) {
        showErrorSnackBar(context, error);
      }
    });

    return AdminPageBody(
      title: AdminCopy.alerts,
      subtitle: AdminCopy.alertsSubtitle,
      action: FilledButton(
        style: AdminButtonStyles.primary,
        onPressed: () => context.go(AppRoutes.adminAlertNew),
        child: const Text('⊕ ${AdminCopy.createAlert}'),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const AdminNotice(text: AdminCopy.alertNotice),
          AdminSearchField(
            controller: _recipient,
            hint: AdminCopy.recipientFilterHint,
          ),
          if (_filterError != null)
            AdminNotice(text: _filterError!, danger: true),
          const SizedBox(height: 8),
          Align(
            alignment: AlignmentDirectional.centerStart,
            child: FilledButton(
              style: AdminButtonStyles.primary,
              onPressed: state.loading ? null : () => _apply(vm),
              child: const Text(AdminCopy.applyFilters),
            ),
          ),
          const SizedBox(height: 17),
          if (state.loading && state.items.isEmpty)
            const Padding(
              padding: EdgeInsets.all(32),
              child: Center(child: CircularProgressIndicator()),
            )
          else if (state.error != null && state.items.isEmpty)
            MyEmptyState(
              type: MyEmptyType.error,
              title: context.tr.error,
              subtitle: state.error!.message,
              primaryActionLabel: context.tr.retry,
              onPrimaryAction: vm.load,
            )
          else if (state.items.isEmpty)
            const MyEmptyState(
              type: MyEmptyType.empty,
              title: AdminCopy.emptyAlerts,
              subtitle: AdminCopy.alertsSubtitle,
            )
          else
            AdminPanel(
              child: Column(
                children: [
                  for (final alert in state.items)
                    AdminTimelineTile(
                      symbol: '⚑',
                      title: alert.title,
                      subtitle:
                          '${alert.recipientUserId}\n${AdminAlertFacts.shortStatus(alert)}',
                      time: formatAdminTime(alert.createdAt),
                      onTap: () => context.go(
                        AppRoutes.adminAlertPath(alert.adminAlertId),
                      ),
                    ),
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

  void _apply(AdminAlertsViewModel vm) {
    final error = AuditLogQueryRules.optionalGuid(_recipient.text);
    if (error != null) {
      setState(() => _filterError = AdminCopy.fieldMessage('guid'));
      return;
    }
    setState(() => _filterError = null);
    vm.applyRecipient(_recipient.text);
  }
}

class CreateAdminAlertPage extends ConsumerStatefulWidget {
  const CreateAdminAlertPage({super.key});

  @override
  ConsumerState<CreateAdminAlertPage> createState() =>
      _CreateAdminAlertPageState();
}

class _CreateAdminAlertPageState extends ConsumerState<CreateAdminAlertPage> {
  final _recipient = TextEditingController();
  final _title = TextEditingController();
  final _body = TextEditingController();

  @override
  void dispose() {
    _recipient.dispose();
    _title.dispose();
    _body.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(createAdminAlertViewModelProvider);
    ref.listen(createAdminAlertViewModelProvider, (prev, next) {
      final error = next.error;
      if (error != null &&
          error != prev?.error &&
          error.displayType != ErrorDisplayType.inline) {
        showErrorSnackBar(context, error);
      }
    });

    return AdminFormScope(
      fields: [_recipient, _title, _body],
      builder: (context, dirty) {
        return AdminLeaveGuard(
          dirty: dirty,
          child: AdminPageBody(
            title: AdminCopy.createAlert,
            subtitle: AdminCopy.alertNotice,
            action: OutlinedButton(
              style: AdminButtonStyles.outline,
              onPressed: () => leaveAdminForm(
                context,
                dirty: dirty,
                location: AppRoutes.adminAlerts,
              ),
              child: const Text('← ${AdminCopy.back}'),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                AdminFormFrame(
                  child: AdminPanel(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        if (state.error != null && state.field == null)
                          AdminNotice(text: state.error!.message, danger: true),
                        AdminField(
                          label: AdminCopy.recipient,
                          controller: _recipient,
                          errorText: adminFieldError(
                            key: 'recipientUserId',
                            field: state.field,
                            message: state.error?.message,
                          ),
                        ),
                        AdminField(
                          label: AdminCopy.alertTitle,
                          controller: _title,
                          errorText: adminFieldError(
                            key: 'title',
                            field: state.field,
                            message: state.error?.message,
                          ),
                        ),
                        AdminField(
                          label: AdminCopy.alertBody,
                          controller: _body,
                          maxLines: 4,
                          errorText: adminFieldError(
                            key: 'body',
                            field: state.field,
                            message: state.error?.message,
                          ),
                        ),
                        FilledButton(
                          style: AdminButtonStyles.primary,
                          onPressed: state.busy
                              ? null
                              : () => ref
                                    .read(
                                      createAdminAlertViewModelProvider
                                          .notifier,
                                    )
                                    .submit(
                                      recipientUserId: _recipient.text,
                                      title: _title.text,
                                      body: _body.text,
                                    ),
                          child: Text(
                            state.busy ? AdminCopy.busy : AdminCopy.save,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
                if (state.result != null) ...[
                  const SizedBox(height: 18),
                  AdminAlertOutcomePanel(alert: state.result!),
                  const SizedBox(height: 12),
                  Align(
                    alignment: AlignmentDirectional.centerStart,
                    child: OutlinedButton(
                      style: AdminButtonStyles.outline,
                      onPressed: () => context.go(
                        AppRoutes.adminAlertPath(state.result!.adminAlertId),
                      ),
                      child: const Text(AdminCopy.openSavedAlert),
                    ),
                  ),
                ],
              ],
            ),
          ),
        );
      },
    );
  }
}

class AdminAlertDetailPage extends ConsumerWidget {
  const AdminAlertDetailPage({super.key, required this.alertId});

  final String alertId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(adminAlertDetailViewModelProvider(alertId));
    final vm = ref.read(adminAlertDetailViewModelProvider(alertId).notifier);
    ref.listen(adminAlertDetailViewModelProvider(alertId), (prev, next) {
      final error = next.error;
      if (error == null || error == prev?.error) return;
      if (error.displayType != ErrorDisplayType.inline) {
        showErrorSnackBar(context, error);
      }
    });

    return AdminPageBody(
      title: AdminCopy.alertDetail,
      subtitle: AdminCopy.alertsSubtitle,
      action: OutlinedButton(
        style: AdminButtonStyles.outline,
        onPressed: () => context.go(AppRoutes.adminAlerts),
        child: const Text('← ${AdminCopy.back}'),
      ),
      child: state.loading
          ? const Padding(
              padding: EdgeInsets.all(32),
              child: Center(child: CircularProgressIndicator()),
            )
          : state.alert == null
          ? MyEmptyState(
              type: MyEmptyType.error,
              title: context.tr.error,
              subtitle: state.error?.message ?? AdminCopy.emptyAlerts,
              primaryActionLabel: context.tr.retry,
              onPrimaryAction: vm.load,
            )
          : Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Align(
                  alignment: AlignmentDirectional.centerStart,
                  child: OutlinedButton(
                    style: AdminButtonStyles.outline,
                    onPressed: vm.load,
                    child: const Text(AdminCopy.refresh),
                  ),
                ),
                const SizedBox(height: 12),
                AdminAlertOutcomePanel(alert: state.alert!),
              ],
            ),
    );
  }
}
