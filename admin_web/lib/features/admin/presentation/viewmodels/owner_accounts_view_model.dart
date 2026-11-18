import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/session/auth_session_controller.dart';
import '../../../../core/session/session_epoch.dart';
import '../../domain/entities/owner_account_entity.dart';
import '../admin_write_state.dart';
import '../providers/admin_providers.dart';

class OwnerAccountsState {
  const OwnerAccountsState({
    this.accounts = const [],
    this.loading = false,
    this.error,
  });

  final List<OwnerAccountEntity> accounts;
  final bool loading;
  final ErrorState? error;

  OwnerAccountsState copyWith({
    List<OwnerAccountEntity>? accounts,
    bool? loading,
    ErrorState? error,
    bool clearError = false,
  }) {
    return OwnerAccountsState(
      accounts: accounts ?? this.accounts,
      loading: loading ?? this.loading,
      error: clearError ? null : (error ?? this.error),
    );
  }
}

class OwnerAccountsViewModel extends AutoDisposeNotifier<OwnerAccountsState> {
  final _life = DisposeFlag();
  var _ticket = 0;

  @override
  OwnerAccountsState build() {
    _life.attach(ref);
    ref.listen(authSessionProvider, (previous, next) {
      if (previous?.isAuthenticated == true && !next.isAuthenticated) {
        _ticket++;
        state = const OwnerAccountsState();
      }
    });
    Future.microtask(load);
    return const OwnerAccountsState(loading: true);
  }

  Future<void> load() async {
    final ticket = ++_ticket;
    final generation = ref.read(authSessionProvider).generation;
    if (!ref.read(authSessionProvider).isAuthenticated) {
      state = const OwnerAccountsState();
      return;
    }
    state = state.copyWith(loading: state.accounts.isEmpty, clearError: true);
    final result = await ref.read(listOwnerAccountsUseCaseProvider)();
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
          accounts:
              failure is ForbiddenFailure || failure.requiresReauthentication
              ? const []
              : state.accounts,
        );
      case Success(:final data):
        state = OwnerAccountsState(accounts: data);
    }
  }
}

final ownerAccountsViewModelProvider =
    AutoDisposeNotifierProvider<OwnerAccountsViewModel, OwnerAccountsState>(
      OwnerAccountsViewModel.new,
    );
