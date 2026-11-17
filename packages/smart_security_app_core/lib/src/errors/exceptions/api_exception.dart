// =============================================================================
// api_exception.dart
// =============================================================================

// يحتوي على أنواع الأخطاء الداخلية:
// NetworkException - TimeoutException
// HttpException - ParseException -  UnexpectedException

// يمثل الأخطاء قبل تحويلها إلى Failure.
// هذا الفصل مفيد لوضع منطق التحويل بشكل نظيف.

// تعريف استثناءات طبقة Data Layer.
//
// التدفق:
//   DioException / API Error
//          ↓
//   ApiException
//          ↓
//   ErrorMapper
//          ↓
//   Failure
//

// سبب الفصل بين ApiException و Failure:
// - ApiException تستخدم داخل Data Layer وتمثل تفاصيل مصدر الخطأ.
// - Failure تستخدم كـ Contract يصل إلى Domain / Presentation.
// - الفصل يسهل الاختبار، الـ Logging، وتغيير مصدر البيانات.
// =============================================================================

/// Base class لجميع استثناءات API القادمة من Data Layer.
sealed class ApiException implements Exception {
  const ApiException();
}

// =============================================================================
// Network Exception
// =============================================================================

/// خطأ في الاتصال بالشبكة مثل:
/// - انقطاع الاتصال.
/// - فشل DNS.
/// - عدم الوصول إلى الخادم.
class NetworkException extends ApiException {
  const NetworkException({this.details});

  /// معلومات تقنية تستخدم في Logging فقط.
  final String? details;

  @override
  String toString() => 'NetworkException: ${details ?? 'No connection'}';
}

// =============================================================================
// Timeout Exception
// =============================================================================

/// انتهاء المهلة المحددة للطلب قبل الحصول على استجابة.
class TimeoutException extends ApiException {
  const TimeoutException({this.durationMs});

  /// مدة الانتظار التي انتهت قبل فشل الطلب.
  final int? durationMs;

  @override
  String toString() => 'TimeoutException: ${durationMs}ms';
}

// =============================================================================
// HTTP Exception
// =============================================================================

/// خطأ صادر من HTTP Response.
///
/// يمثل أخطاء مثل:
/// - 401 Unauthorized
/// - 403 Forbidden
/// - 404 Not Found
/// - 422 Validation Error
/// - 500 Server Error
class HttpException extends ApiException {
  const HttpException({
    required this.statusCode,
    this.errorCode,
    this.message,
    this.validationErrors = const {},
    this.rawBody,
  });

  /// رمز HTTP القادم من الخادم.
  final int statusCode;

  /// رمز الخطأ الخاص بالـ Backend.
  final String? errorCode;

  /// رسالة الخطأ القادمة من الخادم.
  final String? message;

  /// أخطاء التحقق من البيانات (Validation).
  final Map<String, List<String>> validationErrors;

  /// الاستجابة الخام للاستخدام في Logging.
  final String? rawBody;

  /// هل انتهت صلاحية المصادقة؟
  bool get isUnauthorized => statusCode == 401;

  /// هل المستخدم لا يملك الصلاحية؟
  bool get isForbidden => statusCode == 403;

  /// هل الخطأ متعلق بالبيانات المدخلة؟
  bool get isValidation => statusCode == 422;

  /// هل الخطأ صادر من الخادم؟
  bool get isServerError => statusCode >= 500;

  @override
  String toString() => 'HttpException[$statusCode]: $errorCode - $message';
}

// =============================================================================
// Parse Exception
// =============================================================================

/// فشل تحويل Response JSON إلى Model متوقع.
class ParseException extends ApiException {
  const ParseException({required this.details, this.rawBody, this.stackTrace});

  /// وصف سبب فشل التحليل.
  final String details;

  /// Response الخام للمساعدة في تتبع المشكلة.
  final String? rawBody;

  /// Stack trace الأصلي وقت فشل الـ parsing، لأغراض الـ logging فقط.
  final StackTrace? stackTrace;

  @override
  String toString() => 'ParseException: $details';
}

// =============================================================================
// Unexpected Exception
// =============================================================================

/// خطأ غير متوقع لم يتم تصنيفه ضمن الأخطاء المعروفة.
class UnexpectedException extends ApiException {
  const UnexpectedException({required this.exception, this.stackTrace});

  final Object exception;

  final StackTrace? stackTrace;

  @override
  String toString() => 'UnexpectedException: $exception';
}
