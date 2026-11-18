import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

/// A write is successful only when the repository returns [Success].
class AdminWriteState {
  const AdminWriteState({
    this.busy = false,
    this.succeeded = false,
    this.error,
    this.field,
  });

  final bool busy;
  final bool succeeded;
  final ErrorState? error;
  final String? field;

  AdminWriteState copyWith({
    bool? busy,
    bool? succeeded,
    ErrorState? error,
    String? field,
    bool clearError = false,
    bool clearField = false,
  }) {
    return AdminWriteState(
      busy: busy ?? this.busy,
      succeeded: succeeded ?? this.succeeded,
      error: clearError ? null : (error ?? this.error),
      field: clearField ? null : (field ?? this.field),
    );
  }

  static AdminWriteState fromFailure(Failure failure) {
    return AdminWriteState(error: FailureHandler.handle(failure));
  }

  static AdminWriteState validation(String field, String message) {
    return AdminWriteState(
      field: field,
      error: ErrorState(message: message, displayType: ErrorDisplayType.inline),
    );
  }
}

bool adminWriteAccepted(Result<dynamic> result) => result is Success;

/// Tracks notifier disposal without relying on a Riverpod `mounted` getter.
class DisposeFlag {
  bool disposed = false;

  void attach(Ref ref) {
    disposed = false;
    ref.onDispose(() => disposed = true);
  }
}
