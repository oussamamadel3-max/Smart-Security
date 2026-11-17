// =============================================================================
// failure.dart
// =============================================================================
// تعريف جميع حالات الفشل التي يمكن أن تصل إلى Domain / Presentation.
//
// المبدأ:
// الـ UI يتعامل مع Failure فقط ولا يعتمد على مصدر الخطأ:
// - Network
// - Server
// - Validation
// - Authentication
//
// الفصل بين Exception و Failure يحافظ على استقلالية الطبقات ضمن
// Clean Architecture.
//
// sealed class يسمح للـ Dart بالتحقق من اكتمال حالات switch.
// =============================================================================

import 'package:freezed_annotation/freezed_annotation.dart';

part 'failure.freezed.dart';

/// Base sealed class لجميع حالات الفشل في التطبيق.
@freezed
sealed class Failure with _$Failure {
  // ==========================================================================
  // Network Failures
  // ==========================================================================

  /// فشل الاتصال بالشبكة أو الوصول إلى الخادم.
  const factory Failure.network({
    @Default('لا يوجد اتصال بالإنترنت') String message,

    /// إمكانية إعادة المحاولة تلقائياً.
    @Default(true) bool retryable,
  }) = NetworkFailure;

  /// انتهاء المهلة أثناء تنفيذ الطلب.
  const factory Failure.timeout({
    @Default('انتهت مهلة الاتصال') String message,
    @Default(true) bool retryable,
  }) = TimeoutFailure;

  // ==========================================================================
  // Authentication & Authorization Failures
  // ==========================================================================

  /// الجلسة غير صالحة أو انتهت صلاحية Token.
  const factory Failure.unauthorized({
    @Default('انتهت جلستك، يرجى تسجيل الدخول') String message,
  }) = UnauthorizedFailure;

  /// المستخدم لا يملك الصلاحية المطلوبة.
  const factory Failure.forbidden({
    @Default('ليس لديك صلاحية للقيام بهذا الإجراء') String message,
  }) = ForbiddenFailure;

  // ==========================================================================
  // Server Failures
  // ==========================================================================

  /// خطأ داخلي صادر من الخادم.
  const factory Failure.server({
    @Default('حدث خطأ في الخادم') String message,

    /// رمز الخطأ القادم من Backend.
    String? errorCode,

    @Default(false) bool retryable,
  }) = ServerFailure;

  /// المورد المطلوب غير موجود.
  const factory Failure.notFound({
    @Default('البيانات المطلوبة غير موجودة') String message,
    String? resourceType,
  }) = NotFoundFailure;

  /// تعارض في البيانات مثل تكرار قيمة فريدة.
  const factory Failure.conflict({
    @Default('البيانات موجودة مسبقاً') String message,
    String? field,
  }) = ConflictFailure;

  // ==========================================================================
  // Validation Failures
  // ==========================================================================

  /// فشل التحقق من البيانات المدخلة.
  ///
  /// يحتوي errors على:
  /// field -> قائمة الأخطاء الخاصة به.
  const factory Failure.validation({
    @Default('يوجد أخطاء في البيانات المدخلة') String message,
    @Default({}) Map<String, List<String>> errors,
  }) = ValidationFailure;

  // ==========================================================================
  // Response Parsing Failures
  // ==========================================================================

  /// تعذر تحويل Response إلى Model متوقع.
  const factory Failure.parsing({
    @Default('تعذّر معالجة البيانات الواردة') String message,
    String? details,
  }) = ParsingFailure;

  // ==========================================================================
  // Generic HTTP Failures
  // ==========================================================================

  /// خطأ HTTP غير مصنف ضمن الحالات السابقة.
  const factory Failure.httpError({
    required int statusCode,
    @Default('حدث خطأ غير متوقع') String message,
    String? errorCode,
  }) = HttpErrorFailure;

  // ==========================================================================
  // Unexpected Failures
  // ==========================================================================

  /// خطأ غير متوقع يجب تسجيل تفاصيله للتحليل.
  const factory Failure.unexpected({
    @Default('حدث خطأ غير متوقع') String message,
    Object? exception,
    StackTrace? stackTrace,
  }) = UnexpectedFailure;
}

// =============================================================================
// Failure Extensions
// =============================================================================
// خصائص مشتقة تساعد الطبقات العليا على التعامل مع الأخطاء بشكل موحد.
// =============================================================================

extension FailureExtension on Failure {
  /// الرسالة المناسبة للعرض في UI.
  String get displayMessage => switch (this) {
    NetworkFailure(:final message) => message,
    TimeoutFailure(:final message) => message,
    UnauthorizedFailure(:final message) => message,
    ForbiddenFailure(:final message) => message,
    ServerFailure(:final message) => message,
    NotFoundFailure(:final message) => message,
    ConflictFailure(:final message) => message,
    ValidationFailure(:final message) => message,
    ParsingFailure(:final message) => message,
    HttpErrorFailure(:final message) => message,
    UnexpectedFailure(:final message) => message,
  };

  /// هل يمكن إعادة محاولة العملية؟
  ///
  /// يعتمد القرار على نوع الخطأ وإعداد retryable.
  bool get isRetryable => switch (this) {
    NetworkFailure(:final retryable) => retryable,
    TimeoutFailure(:final retryable) => retryable,
    ServerFailure(:final retryable) => retryable,
    _ => false,
  };

  /// هل يحتاج المستخدم إلى تسجيل الدخول مرة أخرى؟
  bool get requiresReauthentication => this is UnauthorizedFailure;

  /// هل يجب إرسال الخطأ إلى نظام المراقبة؟
  ///
  /// مثل:
  /// - Crashlytics
  /// - Sentry
  bool get shouldLog => switch (this) {
    NetworkFailure() => true,
    TimeoutFailure() => true,
    ServerFailure() => true,
    ParsingFailure() => true,
    UnexpectedFailure() => true,
    _ => false,
  };
}
