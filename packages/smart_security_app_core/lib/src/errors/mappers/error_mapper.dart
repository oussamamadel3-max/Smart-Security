import '../codes/api_error_codes.dart';
import '../exceptions/api_exception.dart';
import '../failures/failure.dart';

/// Maps [ApiException] → [Failure] for Domain / Presentation.
abstract final class ErrorMapper {
  static Failure map(ApiException exception) {
    return switch (exception) {
      NetworkException() => const Failure.network(),
      TimeoutException() => const Failure.timeout(),
      HttpException() => _mapHttpException(exception),
      ParseException(:final details) => Failure.parsing(details: details),
      UnexpectedException(:final exception, :final stackTrace) =>
        Failure.unexpected(exception: exception, stackTrace: stackTrace),
    };
  }

  static Failure _mapHttpException(HttpException e) {
    final message = ApiErrorCodes.getMessage(e.errorCode);

    return switch (e.statusCode) {
      401 => UnauthorizedFailure(
          message: ApiErrorCodes.getMessage(
            e.errorCode,
            fallback: 'انتهت جلستك، يرجى تسجيل الدخول',
          ),
        ),
      403 => ForbiddenFailure(
          message: ApiErrorCodes.getMessage(
            e.errorCode,
            fallback: 'ليس لديك صلاحية للقيام بهذا الإجراء',
          ),
        ),
      404 => NotFoundFailure(
          message: ApiErrorCodes.getMessage(
            e.errorCode,
            fallback: 'البيانات المطلوبة غير موجودة',
          ),
        ),
      409 => ConflictFailure(
          message: ApiErrorCodes.getMessage(
            e.errorCode,
            fallback: 'البيانات موجودة مسبقاً',
          ),
        ),
      400 when e.validationErrors.isNotEmpty => ValidationFailure(
          message: message,
          errors: e.validationErrors,
        ),
      422 => ValidationFailure(
          message: ApiErrorCodes.getMessage(
            e.errorCode,
            fallback: 'يوجد أخطاء في البيانات المدخلة',
          ),
          errors: e.validationErrors,
        ),
      503 => ServerFailure(
          message: ApiErrorCodes.getMessage(
            e.errorCode,
            fallback: 'الخدمة غير متاحة مؤقتاً',
          ),
          errorCode: e.errorCode,
          retryable: true,
        ),
      >= 500 => ServerFailure(
          message: ApiErrorCodes.getMessage(
            e.errorCode,
            fallback: 'حدث خطأ في الخادم',
          ),
          errorCode: e.errorCode,
          retryable: false,
        ),
      _ => HttpErrorFailure(
          statusCode: e.statusCode,
          message: ApiErrorCodes.getMessage(
            e.errorCode,
            fallback: 'حدث خطأ غير متوقع',
          ),
          errorCode: e.errorCode,
        ),
    };
  }
}
