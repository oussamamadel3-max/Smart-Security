import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/session/auth_session_controller.dart';
import '../../../../core/session/session_epoch.dart';
import '../../domain/entities/admin_device_entity.dart';
import '../../domain/entities/admin_mutation_entities.dart';
import '../../domain/register_device_input.dart';
import '../admin_copy.dart';
import '../admin_write_state.dart';
import '../providers/admin_providers.dart';

class AdminDevicesState {
  const AdminDevicesState({
    this.devices = const [],
    this.loading = false,
    this.error,
  });

  final List<AdminDeviceEntity> devices;
  final bool loading;
  final ErrorState? error;

  AdminDevicesState copyWith({
    List<AdminDeviceEntity>? devices,
    bool? loading,
    ErrorState? error,
    bool clearError = false,
  }) {
    return AdminDevicesState(
      devices: devices ?? this.devices,
      loading: loading ?? this.loading,
      error: clearError ? null : (error ?? this.error),
    );
  }
}

class AdminDevicesViewModel extends AutoDisposeNotifier<AdminDevicesState> {
  final _life = DisposeFlag();
  var _ticket = 0;

  @override
  AdminDevicesState build() {
    _life.attach(ref);
    ref.listen(authSessionProvider, (previous, next) {
      if (previous?.isAuthenticated == true && !next.isAuthenticated) {
        _ticket++;
        state = const AdminDevicesState();
      }
    });
    Future.microtask(load);
    return const AdminDevicesState(loading: true);
  }

  Future<void> load() async {
    final ticket = ++_ticket;
    final generation = ref.read(authSessionProvider).generation;
    if (!ref.read(authSessionProvider).isAuthenticated) {
      state = const AdminDevicesState();
      return;
    }
    state = state.copyWith(loading: state.devices.isEmpty, clearError: true);
    final result = await ref.read(listAdminDevicesUseCaseProvider)();
    if (_life.disposed || ticket != _ticket) return;
    if (!sessionResultApplies(
      capturedGeneration: generation,
      session: ref.read(authSessionProvider),
    )) {
      return;
    }
    switch (result) {
      case Failure_(:final failure):
        state = state.copyWith(
          loading: false,
          error: FailureHandler.handle(failure, onRetry: load),
          devices:
              failure is ForbiddenFailure || failure.requiresReauthentication
              ? const []
              : state.devices,
        );
      case Success(:final data):
        state = AdminDevicesState(devices: data);
    }
  }
}

final adminDevicesViewModelProvider =
    AutoDisposeNotifierProvider<AdminDevicesViewModel, AdminDevicesState>(
      AdminDevicesViewModel.new,
    );

class RegisterDeviceState {
  const RegisterDeviceState({
    this.busy = false,
    this.succeeded = false,
    this.error,
    this.field,
    this.createdId,
  });

  final bool busy;
  final bool succeeded;
  final ErrorState? error;
  final String? field;
  final String? createdId;
}

class RegisterDeviceViewModel extends AutoDisposeNotifier<RegisterDeviceState> {
  final _life = DisposeFlag();

  @override
  RegisterDeviceState build() {
    _life.attach(ref);
    return const RegisterDeviceState();
  }

  Future<void> submit(RegisterDeviceInput input) async {
    final field = RegisterDeviceRules.validate(input);
    if (field != null) {
      state = RegisterDeviceState(
        field: field,
        error: ErrorState(
          message: AdminCopy.fieldMessage(field),
          displayType: ErrorDisplayType.inline,
        ),
      );
      return;
    }
    state = const RegisterDeviceState(busy: true);
    final result = await ref.read(registerAdminDeviceUseCaseProvider)(input);
    if (_life.disposed) return;
    switch (result) {
      case Failure_(:final failure):
        state = RegisterDeviceState(error: FailureHandler.handle(failure));
      case Success(:final data):
        state = RegisterDeviceState(succeeded: true, createdId: data.id);
    }
  }
}

final registerDeviceViewModelProvider =
    AutoDisposeNotifierProvider<RegisterDeviceViewModel, RegisterDeviceState>(
      RegisterDeviceViewModel.new,
    );

class AdminDeviceDetailState {
  const AdminDeviceDetailState({
    this.device,
    this.loading = false,
    this.write = const AdminWriteState(),
    this.unlinkResult,
    this.archiveResult,
  });

  final AdminDeviceEntity? device;
  final bool loading;
  final AdminWriteState write;
  final UnlinkedDeviceEntity? unlinkResult;
  final ArchivedDeviceEntity? archiveResult;

  AdminDeviceDetailState copyWith({
    AdminDeviceEntity? device,
    bool? loading,
    AdminWriteState? write,
    UnlinkedDeviceEntity? unlinkResult,
    ArchivedDeviceEntity? archiveResult,
    bool clearDevice = false,
  }) {
    return AdminDeviceDetailState(
      device: clearDevice ? null : (device ?? this.device),
      loading: loading ?? this.loading,
      write: write ?? this.write,
      unlinkResult: unlinkResult ?? this.unlinkResult,
      archiveResult: archiveResult ?? this.archiveResult,
    );
  }
}

class AdminDeviceDetailViewModel
    extends AutoDisposeFamilyNotifier<AdminDeviceDetailState, String> {
  final _life = DisposeFlag();

  @override
  AdminDeviceDetailState build(String arg) {
    _life.attach(ref);
    Future.microtask(load);
    return const AdminDeviceDetailState(loading: true);
  }

  Future<void> load() async {
    state = state.copyWith(loading: state.device == null);
    final result = await ref.read(getAdminDeviceUseCaseProvider)(arg);
    if (_life.disposed) return;
    switch (result) {
      case Failure_(:final failure):
        state = state.copyWith(
          loading: false,
          clearDevice:
              failure is ForbiddenFailure ||
              failure is NotFoundFailure ||
              failure.requiresReauthentication,
          write: AdminWriteState.fromFailure(failure),
        );
      case Success(:final data):
        state = AdminDeviceDetailState(
          device: data,
          unlinkResult: state.unlinkResult,
          archiveResult: state.archiveResult,
        );
    }
  }

  Future<void> unlink() async {
    state = state.copyWith(write: const AdminWriteState(busy: true));
    final result = await ref.read(unlinkAdminDeviceUseCaseProvider)(arg);
    if (_life.disposed) return;
    switch (result) {
      case Failure_(:final failure):
        state = state.copyWith(write: AdminWriteState.fromFailure(failure));
      case Success(:final data):
        state = state.copyWith(
          write: const AdminWriteState(succeeded: true),
          unlinkResult: data,
        );
        await load();
    }
  }

  Future<void> archive() async {
    state = state.copyWith(write: const AdminWriteState(busy: true));
    final result = await ref.read(archiveAdminDeviceUseCaseProvider)(arg);
    if (_life.disposed) return;
    switch (result) {
      case Failure_(:final failure):
        state = state.copyWith(write: AdminWriteState.fromFailure(failure));
      case Success(:final data):
        state = state.copyWith(
          write: const AdminWriteState(succeeded: true),
          archiveResult: data,
        );
        await load();
    }
  }
}

final adminDeviceDetailViewModelProvider =
    AutoDisposeNotifierProvider.family<
      AdminDeviceDetailViewModel,
      AdminDeviceDetailState,
      String
    >(AdminDeviceDetailViewModel.new);
