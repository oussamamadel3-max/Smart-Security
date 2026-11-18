import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../domain/create_owner_input.dart';
import '../../domain/entities/owner_account_entity.dart';
import '../admin_copy.dart';
import '../admin_write_state.dart';
import '../providers/admin_providers.dart';

class CreateOwnerState {
  const CreateOwnerState({
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

class CreateOwnerViewModel extends AutoDisposeNotifier<CreateOwnerState> {
  final _life = DisposeFlag();

  @override
  CreateOwnerState build() {
    _life.attach(ref);
    return const CreateOwnerState();
  }

  Future<void> submit({
    required String userName,
    required String fullName,
    required String password,
    String? email,
  }) async {
    final field = OwnerAccountRules.validateCreate(
      userName: userName,
      fullName: fullName,
      password: password,
      email: email,
    );
    if (field != null) {
      state = CreateOwnerState(
        field: field,
        error: ErrorState(
          message: AdminCopy.fieldMessage(field),
          displayType: ErrorDisplayType.inline,
        ),
      );
      return;
    }

    state = const CreateOwnerState(busy: true);
    final result = await ref.read(createOwnerAccountUseCaseProvider)(
      CreateOwnerInput(
        userName: userName,
        fullName: fullName,
        password: password,
        email: email,
      ),
    );
    if (_life.disposed) return;
    switch (result) {
      case Failure_(:final failure):
        state = CreateOwnerState(error: FailureHandler.handle(failure));
      case Success(:final data):
        state = CreateOwnerState(succeeded: true, createdId: data.id);
    }
  }
}

final createOwnerViewModelProvider =
    AutoDisposeNotifierProvider<CreateOwnerViewModel, CreateOwnerState>(
      CreateOwnerViewModel.new,
    );

class OwnerAccountDetailState {
  const OwnerAccountDetailState({
    this.account,
    this.loading = false,
    this.write = const AdminWriteState(),
  });

  final OwnerAccountEntity? account;
  final bool loading;
  final AdminWriteState write;

  OwnerAccountDetailState copyWith({
    OwnerAccountEntity? account,
    bool? loading,
    AdminWriteState? write,
    bool clearAccount = false,
  }) {
    return OwnerAccountDetailState(
      account: clearAccount ? null : (account ?? this.account),
      loading: loading ?? this.loading,
      write: write ?? this.write,
    );
  }
}

class OwnerAccountDetailViewModel
    extends AutoDisposeFamilyNotifier<OwnerAccountDetailState, String> {
  final _life = DisposeFlag();

  @override
  OwnerAccountDetailState build(String arg) {
    _life.attach(ref);
    Future.microtask(load);
    return const OwnerAccountDetailState(loading: true);
  }

  Future<void> load() async {
    state = state.copyWith(loading: state.account == null);
    final result = await ref.read(getOwnerAccountUseCaseProvider)(arg);
    if (_life.disposed) return;
    switch (result) {
      case Failure_(:final failure):
        state = state.copyWith(
          loading: false,
          clearAccount: _hideAccount(failure),
          write: AdminWriteState.fromFailure(failure),
        );
      case Success(:final data):
        state = OwnerAccountDetailState(account: data);
    }
  }

  Future<void> save({required String fullName, String? email}) async {
    final field = OwnerAccountRules.validateUpdate(
      fullName: fullName,
      email: email,
    );
    if (field != null) {
      state = state.copyWith(
        write: AdminWriteState.validation(field, AdminCopy.fieldMessage(field)),
      );
      return;
    }
    await _run(
      () => ref.read(updateOwnerAccountUseCaseProvider)(
        arg,
        UpdateOwnerInput(fullName: fullName, email: email),
      ),
    );
  }

  Future<void> disable() async {
    await _run(() => ref.read(disableOwnerAccountUseCaseProvider)(arg));
  }

  Future<void> archive() async {
    await _run(() => ref.read(archiveOwnerAccountUseCaseProvider)(arg));
  }

  Future<void> _run(Future<Result<dynamic>> Function() action) async {
    state = state.copyWith(write: const AdminWriteState(busy: true));
    final result = await action();
    if (_life.disposed) return;
    if (!adminWriteAccepted(result)) {
      state = state.copyWith(
        write: AdminWriteState.fromFailure(result.failureOrNull!),
      );
      return;
    }
    await load();
    if (_life.disposed) return;
    if (state.write.error == null) {
      state = state.copyWith(write: const AdminWriteState(succeeded: true));
    }
  }
}

bool _hideAccount(Failure failure) {
  return failure is ForbiddenFailure ||
      failure is NotFoundFailure ||
      failure.requiresReauthentication;
}

final ownerAccountDetailViewModelProvider =
    AutoDisposeNotifierProvider.family<
      OwnerAccountDetailViewModel,
      OwnerAccountDetailState,
      String
    >(OwnerAccountDetailViewModel.new);
