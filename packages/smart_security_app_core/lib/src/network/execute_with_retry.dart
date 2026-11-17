import '../errors/failures/failure.dart';
import '../errors/result/result.dart';

/// Retries a GET-style [action] when the failure is marked retryable.
///
/// POST/DELETE callers must not use this unless the request has an explicit
/// idempotency key.
Future<Result<T>> executeWithRetry<T>(
  Future<Result<T>> Function() action, {
  int maxAttempts = 3,
  Duration initialDelay = const Duration(milliseconds: 250),
}) async {
  late Result<T> result;
  for (var attempt = 0; attempt < maxAttempts; attempt++) {
    result = await action();
    if (result.isSuccess) return result;

    final failure = result.failureOrNull;
    final canRetry =
        failure != null && failure.isRetryable && attempt < maxAttempts - 1;
    if (!canRetry) return result;

    await Future<void>.delayed(initialDelay * (attempt + 1));
  }
  return result;
}
