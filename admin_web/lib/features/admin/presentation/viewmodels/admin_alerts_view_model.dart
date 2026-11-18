import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/session/auth_session_controller.dart';
import '../../domain/admin_alert_query.dart';
import '../../domain/create_admin_alert_input.dart';
import '../../domain/entities/admin_alert_entity.dart';
import '../admin_copy.dart';
import '../admin_write_state.dart';
import '../providers/admin_providers.dart';

class AdminAlertsState {
  const AdminAlertsState({
    this.items = const [],
    this.page = 1,
    this.pageSize = 20,
    this.totalCount = 0,
    this.query = const AdminAlertQuery(),
    this.loading = false,
    this.error,
  });

  final List<AdminAlertEntity> items;
  final int page;
  final int pageSize;
  final int totalCount;
  final AdminAlertQuery query;
  final bool loading;
  final ErrorState? error;

  bool get hasPrevious => page > 1;

  bool get hasNext => pageSize > 0 && page * pageSize < totalCount;

  AdminAlertsState copyWith({
    List<AdminAlertEntity>? items,
    int? page,
    int? pageSize,
    int? totalCount,
    AdminAlertQuery? query,
    bool? loading,
    ErrorState? error,
    bool clearError = false,
  }) {
    return AdminAlertsState(
      items: items ?? this.items,
      page: page ?? this.page,
      pageSize: pageSize ?? this.pageSize,
      totalCount: totalCount ?? this.totalCount,
      query: query ?? this.query,
      loading: loading ?? this.loading,
      error: clearError ? null : (error ?? this.error),
    );
  }
}

class AdminAlertsViewModel extends AutoDisposeNotifier<AdminAlertsState> {
  final _life = DisposeFlag();
  var _ticket = 0;

  @override
  AdminAlertsState build() {
    _life.attach(ref);
    ref.listen(authSessionProvider, (previous, next) {
      if (previous?.isAuthenticated == true && !next.isAuthenticated) {
        _ticket += 1;
        state = const AdminAlertsState();
      }
    });
    Future.microtask(load);
    return const AdminAlertsState(loading: true);
  }

  Future<void> load() => _load(state.query);

  Future<void> applyRecipient(String? recipientUserId) {
    final text = recipientUserId?.trim() ?? '';
    return _load(
      AdminAlertQuery(
        recipientUserId: text.isEmpty ? null : text,
        pageSize: state.query.pageSize,
      ),
    );
  }

  Future<void> openPage(int page) => _load(state.query.copyWith(page: page));

  Future<void> _load(AdminAlertQuery query) async {
    final ticket = ++_ticket;
    state = state.copyWith(
      loading: state.items.isEmpty,
      query: query,
      clearError: true,
    );
    final result = await ref.read(listAdminAlertsUseCaseProvider)(query);
    if (_life.disposed || ticket != _ticket) return;
    switch (result) {
      case Failure_(:final failure):
        final blocked =
            failure is ForbiddenFailure || failure.requiresReauthentication;
        state = state.copyWith(
          loading: false,
          items: blocked ? const [] : state.items,
          error: FailureHandler.handle(failure, onRetry: load),
        );
      case Success(:final data):
        state = AdminAlertsState(
          items: data.items,
          page: data.page,
          pageSize: data.pageSize,
          totalCount: data.totalCount,
          query: query,
        );
    }
  }
}

final adminAlertsViewModelProvider =
    AutoDisposeNotifierProvider<AdminAlertsViewModel, AdminAlertsState>(
      AdminAlertsViewModel.new,
    );

class CreateAdminAlertState {
  const CreateAdminAlertState({
    this.busy = false,
    this.succeeded = false,
    this.error,
    this.field,
    this.result,
  });

  final bool busy;
  final bool succeeded;
  final ErrorState? error;
  final String? field;
  final AdminAlertEntity? result;
}

class CreateAdminAlertViewModel
    extends AutoDisposeNotifier<CreateAdminAlertState> {
  final _life = DisposeFlag();

  @override
  CreateAdminAlertState build() {
    _life.attach(ref);
    return const CreateAdminAlertState();
  }

  Future<void> submit({
    required String recipientUserId,
    required String title,
    required String body,
  }) async {
    final field = AdminAlertRules.validate(
      recipientUserId: recipientUserId,
      title: title,
      body: body,
    );
    if (field != null) {
      state = CreateAdminAlertState(
        field: field,
        error: ErrorState(
          message: AdminCopy.fieldMessage(field),
          displayType: ErrorDisplayType.inline,
        ),
      );
      return;
    }

    state = const CreateAdminAlertState(busy: true);
    final result = await ref.read(createAdminAlertUseCaseProvider)(
      CreateAdminAlertInput(
        recipientUserId: recipientUserId.trim(),
        title: title.trim(),
        body: body.trim(),
      ),
    );
    if (_life.disposed) return;
    switch (result) {
      case Failure_(:final failure):
        state = CreateAdminAlertState(error: FailureHandler.handle(failure));
      case Success(:final data):
        state = CreateAdminAlertState(succeeded: true, result: data);
    }
  }
}

final createAdminAlertViewModelProvider =
    AutoDisposeNotifierProvider<
      CreateAdminAlertViewModel,
      CreateAdminAlertState
    >(CreateAdminAlertViewModel.new);

class AdminAlertDetailState {
  const AdminAlertDetailState({this.alert, this.loading = false, this.error});

  final AdminAlertEntity? alert;
  final bool loading;
  final ErrorState? error;
}

class AdminAlertDetailViewModel
    extends AutoDisposeFamilyNotifier<AdminAlertDetailState, String> {
  final _life = DisposeFlag();

  @override
  AdminAlertDetailState build(String arg) {
    _life.attach(ref);
    Future.microtask(load);
    return const AdminAlertDetailState(loading: true);
  }

  Future<void> load() async {
    state = AdminAlertDetailState(
      alert: state.alert,
      loading: state.alert == null,
    );
    final result = await ref.read(getAdminAlertUseCaseProvider)(arg);
    if (_life.disposed) return;
    switch (result) {
      case Failure_(:final failure):
        final blocked =
            failure is ForbiddenFailure || failure.requiresReauthentication;
        state = AdminAlertDetailState(
          alert: blocked ? null : state.alert,
          error: FailureHandler.handle(failure, onRetry: load),
        );
      case Success(:final data):
        state = AdminAlertDetailState(alert: data);
    }
  }
}

final adminAlertDetailViewModelProvider =
    AutoDisposeNotifierProviderFamily<
      AdminAlertDetailViewModel,
      AdminAlertDetailState,
      String
    >(AdminAlertDetailViewModel.new);
