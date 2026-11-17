import '../error_state.dart';
import '../failures/failure.dart';

abstract final class FailureHandler {
  static ErrorState handle(
    Failure failure, {
    void Function()? onRetry,
    void Function()? onReauth,
  }) {
    if (failure.shouldLog) {
      _logFailure(failure);
    }

    return switch (failure) {
      NetworkFailure(:final message) => ErrorState(
        message: message,
        displayType: ErrorDisplayType.snackbar,
        canRetry: true,
        actionLabel: onRetry != null ? 'إعادة المحاولة' : null,
        onAction: onRetry,
      ),
      TimeoutFailure(:final message) => ErrorState(
        message: message,
        displayType: ErrorDisplayType.snackbar,
        canRetry: true,
        actionLabel: onRetry != null ? 'إعادة المحاولة' : null,
        onAction: onRetry,
      ),
      UnauthorizedFailure(:final message) => ErrorState(
        message: message,
        displayType: ErrorDisplayType.dialog,
        requiresReauth: true,
        actionLabel: 'تسجيل الدخول',
        onAction: onReauth,
      ),
      ForbiddenFailure(:final message) => ErrorState(
        message: message,
        displayType: ErrorDisplayType.snackbar,
      ),
      NotFoundFailure(:final message) => ErrorState(
        message: message,
        displayType: ErrorDisplayType.snackbar,
      ),
      ConflictFailure(:final message) => ErrorState(
        message: message,
        displayType: ErrorDisplayType.snackbar,
      ),
      ValidationFailure(:final message, :final errors) => ErrorState(
        message: message,
        displayType: ErrorDisplayType.inline,
        validationErrors: errors,
      ),
      ParsingFailure(:final message) => ErrorState(
        message: message,
        displayType: ErrorDisplayType.snackbar,
      ),
      ServerFailure(:final message, :final retryable) => ErrorState(
        message: message,
        displayType: ErrorDisplayType.dialog,
        canRetry: retryable,
        actionLabel: retryable && onRetry != null ? 'إعادة المحاولة' : null,
        onAction: retryable ? onRetry : null,
      ),
      HttpErrorFailure(:final message) => ErrorState(
        message: message,
        displayType: ErrorDisplayType.snackbar,
      ),
      UnexpectedFailure(:final message) => ErrorState(
        message: message,
        displayType: ErrorDisplayType.fullScreen,
        canRetry: onRetry != null,
        actionLabel: onRetry != null ? 'إعادة المحاولة' : null,
        onAction: onRetry,
      ),
    };
  }

  static void _logFailure(Failure failure) {
    // ignore: avoid_print
    print('[FailureHandler] Logged: $failure');
  }
}
