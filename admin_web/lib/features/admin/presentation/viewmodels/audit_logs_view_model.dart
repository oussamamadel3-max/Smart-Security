import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/session/auth_session_controller.dart';
import '../../domain/audit_log_query.dart';
import '../../domain/entities/audit_log_entity.dart';
import '../admin_write_state.dart';
import '../providers/admin_providers.dart';

class AuditLogsState {
  const AuditLogsState({
    this.items = const [],
    this.page = 1,
    this.pageSize = 20,
    this.totalCount = 0,
    this.query = const AuditLogQuery(),
    this.loading = false,
    this.error,
  });

  final List<AuditLogEntity> items;
  final int page;
  final int pageSize;
  final int totalCount;
  final AuditLogQuery query;
  final bool loading;
  final ErrorState? error;

  bool get hasPrevious => page > 1;

  bool get hasNext => pageSize > 0 && page * pageSize < totalCount;

  AuditLogsState copyWith({
    List<AuditLogEntity>? items,
    int? page,
    int? pageSize,
    int? totalCount,
    AuditLogQuery? query,
    bool? loading,
    ErrorState? error,
    bool clearError = false,
  }) {
    return AuditLogsState(
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

class AuditLogsViewModel extends AutoDisposeNotifier<AuditLogsState> {
  final _life = DisposeFlag();
  var _ticket = 0;

  @override
  AuditLogsState build() {
    _life.attach(ref);
    ref.listen(authSessionProvider, (previous, next) {
      if (previous?.isAuthenticated == true && !next.isAuthenticated) {
        _ticket += 1;
        state = const AuditLogsState();
      }
    });
    Future.microtask(load);
    return const AuditLogsState(loading: true);
  }

  Future<void> load() => _load(state.query);

  Future<void> apply(AuditLogQuery query) => _load(query.copyWith(page: 1));

  Future<void> openPage(int page) => _load(state.query.copyWith(page: page));

  Future<void> _load(AuditLogQuery query) async {
    final ticket = ++_ticket;
    state = state.copyWith(
      loading: state.items.isEmpty,
      query: query,
      clearError: true,
    );
    final result = await ref.read(queryAuditLogsUseCaseProvider)(query);
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
        state = AuditLogsState(
          items: data.items,
          page: data.page,
          pageSize: data.pageSize,
          totalCount: data.totalCount,
          query: query,
        );
    }
  }
}

final auditLogsViewModelProvider =
    AutoDisposeNotifierProvider<AuditLogsViewModel, AuditLogsState>(
      AuditLogsViewModel.new,
    );

class SelectedAuditLog extends Notifier<AuditLogEntity?> {
  @override
  AuditLogEntity? build() {
    ref.listen(authSessionProvider, (previous, next) {
      if (previous?.isAuthenticated == true && !next.isAuthenticated) {
        state = null;
      }
    });
    return null;
  }

  void select(AuditLogEntity log) => state = log;
}

final selectedAuditLogProvider =
    NotifierProvider<SelectedAuditLog, AuditLogEntity?>(SelectedAuditLog.new);
