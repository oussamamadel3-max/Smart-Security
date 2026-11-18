import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/router/app_routes.dart';
import '../../domain/usecases/open_notification_usecase.dart';
import '../providers/notifications_providers.dart';
import '../viewmodels/notifications_list_view_model.dart';

/// Notifications inbox from Backend + honest OS-permission state (NoOp-aware).
class NotificationsPage extends ConsumerWidget {
  const NotificationsPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final tr = context.tr;
    final theme = Theme.of(context);
    final state = ref.watch(notificationsListViewModelProvider);
    final vm = ref.read(notificationsListViewModelProvider.notifier);

    ref.listen(notificationsListViewModelProvider, (prev, next) {
      final info = next.infoMessage;
      if (info != null && info != prev?.infoMessage) {
        showMySnackBar(context, message: info, type: MySnackType.info);
      }
      final error = next.error;
      if (error != null &&
          error != prev?.error &&
          error.displayType != ErrorDisplayType.inline) {
        showErrorSnackBar(context, error);
      }
    });

    return Scaffold(
      backgroundColor: DesignTokens.background,
      appBar: AppBar(
        title: Text(tr.navNotifications),
        actions: [
          IconButton(
            tooltip: tr.refresh,
            onPressed: state.isLoading ? null : vm.load,
            icon: const Icon(Icons.refresh),
          ),
        ],
      ),
      body: ListView(
        padding: const EdgeInsets.all(DesignTokens.spaceLg),
        children: [
          Container(
            width: double.infinity,
            padding: const EdgeInsets.all(DesignTokens.spaceMd),
            decoration: BoxDecoration(
              color: DesignTokens.noticeBg,
              borderRadius: BorderRadius.circular(DesignTokens.radiusMd),
              border: Border.all(color: DesignTokens.noticeBorder),
            ),
            child: Text(
              tr.notificationsOsPermissionNotice,
              style: theme.textTheme.bodySmall?.copyWith(
                color: DesignTokens.noticeText,
              ),
            ),
          ),
          const SizedBox(height: DesignTokens.spaceMd),
          OutlinedButton.icon(
            onPressed: vm.requestOsPermission,
            icon: const Icon(Icons.notifications_active_outlined),
            label: Text(tr.requestNotificationPermission),
          ),
          if (state.permissionGranted != null) ...[
            const SizedBox(height: DesignTokens.spaceSm),
            Text(
              state.permissionGranted!
                  ? tr.notificationPermissionGranted
                  : tr.notificationPermissionDeniedOrUnavailable,
              style: theme.textTheme.bodySmall?.copyWith(
                color: DesignTokens.muted,
              ),
            ),
          ],
          const SizedBox(height: DesignTokens.spaceXl),
          Text(tr.notificationsInboxTitle, style: theme.textTheme.titleSmall),
          const SizedBox(height: DesignTokens.spaceMd),
          if (state.isLoading && state.items.isEmpty)
            const Center(child: CircularProgressIndicator())
          else if (state.error != null && state.items.isEmpty)
            MyEmptyState(
              type: MyEmptyType.error,
              title: tr.error,
              subtitle: state.error!.message,
              primaryActionLabel: tr.retry,
              onPrimaryAction: vm.load,
            )
          else if (state.items.isEmpty)
            MyEmptyState(
              type: MyEmptyType.empty,
              title: tr.noNotificationsTitle,
              subtitle: tr.noNotificationsSubtitle,
            )
          else
            ...state.items.map(
              (item) => Padding(
                padding: const EdgeInsets.only(bottom: DesignTokens.spaceSm),
                child: ListTile(
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(DesignTokens.radiusMd),
                    side: const BorderSide(color: DesignTokens.border),
                  ),
                  tileColor: DesignTokens.card,
                  title: Text(item.notificationType),
                  subtitle: Text('${item.status} · ${item.createdAt}'),
                  trailing: const Icon(Icons.chevron_left),
                  onTap: () => _open(context, ref, item.notificationId),
                ),
              ),
            ),
        ],
      ),
    );
  }

  Future<void> _open(
    BuildContext context,
    WidgetRef ref,
    String notificationId,
  ) async {
    final result = await ref.read(openNotificationUseCaseProvider)({
      'notificationId': notificationId,
    });
    if (!context.mounted) return;
    switch (result) {
      case OpenNotificationNavigateAlarm(:final deviceId, :final alarmEventId):
        context.push(
          AppRoutes.deviceAlarmEventDetailPath(deviceId, alarmEventId),
        );
      case OpenNotificationDenied(:final failure):
        showErrorSnackBar(
          context,
          FailureHandler.handle(failure),
        );
      case OpenNotificationUnsupported():
        showMySnackBar(
          context,
          message: context.tr.notificationOpenUnsupported,
          type: MySnackType.info,
        );
    }
  }
}
