import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../domain/entities/admin_mutation_entities.dart';
import '../../domain/entities/owner_account_entity.dart';
import '../admin_write_state.dart';
import '../providers/admin_providers.dart';

class OwnershipFormState<T> {
  const OwnershipFormState({
    this.owners = const [],
    this.loading = false,
    this.busy = false,
    this.succeeded = false,
    this.error,
    this.result,
    this.excludedOwnerId,
  });

  final List<OwnerAccountEntity> owners;
  final bool loading;
  final bool busy;
  final bool succeeded;
  final ErrorState? error;
  final T? result;
  final String? excludedOwnerId;

  OwnershipFormState<T> copyWith({
    List<OwnerAccountEntity>? owners,
    bool? loading,
    bool? busy,
    bool? succeeded,
    ErrorState? error,
    T? result,
    String? excludedOwnerId,
    bool clearError = false,
  }) {
    return OwnershipFormState<T>(
      owners: owners ?? this.owners,
      loading: loading ?? this.loading,
      busy: busy ?? this.busy,
      succeeded: succeeded ?? this.succeeded,
      error: clearError ? null : (error ?? this.error),
      result: result ?? this.result,
      excludedOwnerId: excludedOwnerId ?? this.excludedOwnerId,
    );
  }
}

class LinkDeviceViewModel
    extends
        AutoDisposeFamilyNotifier<
          OwnershipFormState<LinkedDeviceEntity>,
          String
        > {
  final _life = DisposeFlag();

  @override
  OwnershipFormState<LinkedDeviceEntity> build(String arg) {
    _life.attach(ref);
    Future.microtask(loadOwners);
    return const OwnershipFormState(loading: true);
  }

  Future<void> loadOwners() async {
    final result = await ref.read(listOwnerAccountsUseCaseProvider)();
    if (_life.disposed) return;
    switch (result) {
      case Failure_(:final failure):
        state = state.copyWith(
          loading: false,
          error: FailureHandler.handle(failure, onRetry: loadOwners),
        );
      case Success(:final data):
        state = state.copyWith(loading: false, owners: data, clearError: true);
    }
  }

  Future<void> submit(String ownerUserId) async {
    if (ownerUserId.trim().isEmpty) return;
    state = state.copyWith(busy: true, succeeded: false, clearError: true);
    final result = await ref.read(linkAdminDeviceUseCaseProvider)(
      deviceId: arg,
      ownerUserId: ownerUserId,
    );
    if (_life.disposed) return;
    _apply(result);
  }

  void _apply(Result<LinkedDeviceEntity> result) {
    if (!adminWriteAccepted(result)) {
      state = state.copyWith(
        busy: false,
        succeeded: false,
        error: FailureHandler.handle(result.failureOrNull!),
      );
      return;
    }
    state = state.copyWith(
      busy: false,
      succeeded: true,
      result: result.dataOrNull,
      clearError: true,
    );
  }
}

final linkDeviceViewModelProvider =
    AutoDisposeNotifierProvider.family<
      LinkDeviceViewModel,
      OwnershipFormState<LinkedDeviceEntity>,
      String
    >(LinkDeviceViewModel.new);

class TransferDeviceViewModel
    extends
        AutoDisposeFamilyNotifier<
          OwnershipFormState<TransferredDeviceEntity>,
          String
        > {
  final _life = DisposeFlag();

  @override
  OwnershipFormState<TransferredDeviceEntity> build(String arg) {
    _life.attach(ref);
    Future.microtask(loadOwners);
    return const OwnershipFormState(loading: true);
  }

  Future<void> loadOwners() async {
    final ownersResult = await ref.read(listOwnerAccountsUseCaseProvider)();
    final deviceResult = await ref.read(getAdminDeviceUseCaseProvider)(arg);
    if (_life.disposed) return;
    final excluded = deviceResult.dataOrNull?.ownerUserId;
    switch (ownersResult) {
      case Failure_(:final failure):
        state = state.copyWith(
          loading: false,
          excludedOwnerId: excluded,
          error: FailureHandler.handle(failure, onRetry: loadOwners),
        );
      case Success(:final data):
        state = state.copyWith(
          loading: false,
          owners: data,
          excludedOwnerId: excluded,
          clearError: true,
        );
    }
  }

  Future<void> submit(String newOwnerUserId) async {
    if (newOwnerUserId.trim().isEmpty) return;
    state = state.copyWith(busy: true, succeeded: false, clearError: true);
    final result = await ref.read(transferAdminDeviceUseCaseProvider)(
      deviceId: arg,
      newOwnerUserId: newOwnerUserId,
    );
    if (_life.disposed) return;
    if (!adminWriteAccepted(result)) {
      state = state.copyWith(
        busy: false,
        succeeded: false,
        error: FailureHandler.handle(result.failureOrNull!),
      );
      return;
    }
    state = state.copyWith(
      busy: false,
      succeeded: true,
      result: result.dataOrNull,
      clearError: true,
    );
  }
}

final transferDeviceViewModelProvider =
    AutoDisposeNotifierProvider.family<
      TransferDeviceViewModel,
      OwnershipFormState<TransferredDeviceEntity>,
      String
    >(TransferDeviceViewModel.new);
