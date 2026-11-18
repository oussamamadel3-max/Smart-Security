import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/push/fcm_messaging_port.dart';
import '../../../../core/session/auth_session_controller.dart';
import '../../../../core/session/auth_session_state.dart';
import '../../data/models/owner_notification_dto.dart';
import '../providers/notifications_providers.dart';

class NotificationsListState {
  const NotificationsListState({
    this.items = const [],
    this.isLoading = false,
    this.permissionGranted,
    this.error,
    this.infoMessage,
  });

  final List<OwnerNotificationListItemDto> items;
  final bool isLoading;
  final bool? permissionGranted;
  final ErrorState? error;
  final String? infoMessage;

  NotificationsListState copyWith({
    List<OwnerNotificationListItemDto>? items,
    bool? isLoading,
    bool? permissionGranted,
    ErrorState? error,
    bool clearError = false,
    String? infoMessage,
    bool clearInfo = false,
  }) {
    return NotificationsListState(
      items: items ?? this.items,
      isLoading: isLoading ?? this.isLoading,
      permissionGranted: permissionGranted ?? this.permissionGranted,
      error: clearError ? null : (error ?? this.error),
      infoMessage: clearInfo ? null : (infoMessage ?? this.infoMessage),
    );
  }
}

class NotificationsListViewModel extends Notifier<NotificationsListState> {
  @override
  NotificationsListState build() {
    ref.listen<AuthSessionState>(authSessionProvider, (previous, next) {
      if (!next.isAuthenticated) {
        state = const NotificationsListState();
        return;
      }
      if (previous?.user?.id != next.user?.id) {
        Future.microtask(load);
      }
    });
    Future.microtask(load);
    return const NotificationsListState(isLoading: true);
  }

  Future<void> load() async {
    if (!ref.read(authSessionProvider).isAuthenticated) {
      state = const NotificationsListState();
      return;
    }
    state = state.copyWith(isLoading: true, clearError: true, clearInfo: true);
    final result =
        await ref.read(ownerNotificationsRepositoryProvider).listMine();
    result.fold(
      onFailure: (failure) {
        state = state.copyWith(
          isLoading: false,
          error: FailureHandler.handle(failure, onRetry: load),
          items: failure.requiresReauthentication || failure is ForbiddenFailure
              ? const []
              : state.items,
        );
      },
      onSuccess: (items) {
        state = state.copyWith(
          isLoading: false,
          items: items,
          clearError: true,
        );
      },
    );
  }

  /// Requests OS permission via [FcmMessagingPort]. NoOp returns false —
  /// UI must not claim Firebase is configured.
  Future<void> requestOsPermission() async {
    final messaging = ref.read(fcmMessagingPortProvider);
    final granted = await messaging.requestPermission();
    state = state.copyWith(
      permissionGranted: granted,
      infoMessage: granted
          ? null
          : 'إذن النظام غير متاح حالياً (منفذ FCM غير مفعّل أو رُفض الإذن). يمكن متابعة السجلات عبر API.',
    );
  }
}

final notificationsListViewModelProvider =
    NotifierProvider<NotificationsListViewModel, NotificationsListState>(
  NotificationsListViewModel.new,
);
