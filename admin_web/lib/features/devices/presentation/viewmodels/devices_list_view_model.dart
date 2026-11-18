import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/session/auth_session_controller.dart';
import '../../../../core/session/auth_session_state.dart';
import '../../../../core/session/session_epoch.dart';
import '../../domain/entities/owner_device_entity.dart';
import '../providers/devices_providers.dart';

class DevicesListState {
  const DevicesListState({
    this.devices = const [],
    this.isLoading = false,
    this.isRefreshing = false,
    this.error,
  });

  final List<OwnerDeviceEntity> devices;
  final bool isLoading;
  final bool isRefreshing;
  final ErrorState? error;

  bool get isEmpty => !isLoading && error == null && devices.isEmpty;

  DevicesListState copyWith({
    List<OwnerDeviceEntity>? devices,
    bool? isLoading,
    bool? isRefreshing,
    ErrorState? error,
    bool clearError = false,
  }) {
    return DevicesListState(
      devices: devices ?? this.devices,
      isLoading: isLoading ?? this.isLoading,
      isRefreshing: isRefreshing ?? this.isRefreshing,
      error: clearError ? null : (error ?? this.error),
    );
  }
}

class DevicesListViewModel extends Notifier<DevicesListState> {
  @override
  DevicesListState build() {
    ref.listen<AuthSessionState>(authSessionProvider, (previous, next) {
      if (!next.isAuthenticated) {
        ref.read(devicesRepositoryProvider).clearCache();
        state = const DevicesListState();
        return;
      }
      if (previous?.user?.id != next.user?.id) {
        ref.read(devicesRepositoryProvider).clearCache();
        state = const DevicesListState();
        Future.microtask(load);
      }
    });

    Future.microtask(load);
    return const DevicesListState(isLoading: true);
  }

  Future<void> load() async {
    final generation = ref.read(authSessionProvider).generation;
    if (!ref.read(authSessionProvider).isAuthenticated) {
      state = const DevicesListState();
      return;
    }

    final refreshing = state.devices.isNotEmpty;
    state = state.copyWith(
      isLoading: !refreshing,
      isRefreshing: refreshing,
      clearError: true,
    );

    final result = await ref.read(listMyDevicesUseCaseProvider)();
    if (!_applyAllowed(generation)) {
      if (!ref.read(authSessionProvider).isAuthenticated) {
        ref.read(devicesRepositoryProvider).clearCache();
        state = const DevicesListState();
      }
      return;
    }
    result.fold(
      onFailure: (failure) {
        state = state.copyWith(
          isLoading: false,
          isRefreshing: false,
          error: FailureHandler.handle(failure, onRetry: load),
          devices:
              failure.requiresReauthentication || failure is ForbiddenFailure
              ? const []
              : state.devices,
        );
        if (failure.requiresReauthentication || failure is ForbiddenFailure) {
          ref.read(devicesRepositoryProvider).clearCache();
        }
      },
      onSuccess: (devices) {
        state = DevicesListState(devices: devices);
      },
    );
  }

  bool _applyAllowed(int generation) {
    return sessionResultApplies(
      capturedGeneration: generation,
      session: ref.read(authSessionProvider),
    );
  }
}

final devicesListViewModelProvider =
    NotifierProvider<DevicesListViewModel, DevicesListState>(
      DevicesListViewModel.new,
    );
