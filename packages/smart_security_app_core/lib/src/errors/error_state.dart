// =============================================================================
// error_state.dart
// =============================================================================
// الحالة النهائية للخطأ التي تستهلكها طبقة UI.
//
// المسؤولية:
// - حمل الرسالة التي ستعرض للمستخدم.
// - تحديد طريقة عرض الخطأ.
// - توفير إجراءات اختيارية مثل Retry أو إعادة تسجيل الدخول.
// - دعم أخطاء Validation المرتبطة بالحقول.
//
// التدفق:
// Failure
//    ↓
// FailureHandler
//    ↓
// ErrorState
//    ↓
// UI Widget
// =============================================================================

/// طريقة عرض الخطأ داخل الواجهة.
enum ErrorDisplayType {
  /// رسالة مؤقتة أسفل الشاشة.
  snackbar,

  /// نافذة تنبيه تحتاج تفاعل المستخدم.
  dialog,

  /// عرض الخطأ بجانب عناصر الإدخال.
  inline,

  /// حالة خطأ كاملة للشاشة.
  fullScreen,
}

/// نموذج موحد لحالة الخطأ في طبقة UI.
///
/// لا يحتوي على تفاصيل تقنية عن مصدر الخطأ،
/// بل يحتوي فقط على المعلومات اللازمة لعرضه والتعامل معه.
class ErrorState {
  /// الرسالة المعروضة للمستخدم.
  final String message;

  /// طريقة عرض الخطأ المناسبة.
  final ErrorDisplayType displayType;

  /// أخطاء الحقول القادمة غالباً من Validation (422).
  ///
  /// مثال:
  /// {
  ///   "email": ["البريد غير صالح"],
  ///   "password": ["كلمة المرور قصيرة"]
  /// }
  final Map<String, List<String>>? validationErrors;

  /// هل يسمح بإعادة تنفيذ العملية.
  final bool canRetry;

  /// هل يحتاج المستخدم إلى تسجيل الدخول مرة أخرى.
  final bool requiresReauth;

  /// النص الظاهر على زر الإجراء.
  ///
  /// مثال:
  /// "إعادة المحاولة"
  /// "تسجيل الدخول"
  final String? actionLabel;

  /// الإجراء المنفذ عند ضغط زر الخطأ.
  final void Function()? onAction;

  const ErrorState({
    required this.message,
    required this.displayType,
    this.validationErrors,
    this.canRetry = false,
    this.requiresReauth = false,
    this.actionLabel,
    this.onAction,
  });

  /// يعيد أول خطأ مرتبط بحقل معين.
  ///
  /// يستخدم مع TextFormField:
  /// ```dart
  /// errorText: errorState.fieldError('email')
  /// ```
  String? fieldError(String field) => validationErrors?[field]?.first;

  /// يتحقق هل يوجد خطأ للحقل المحدد.
  bool hasFieldError(String field) =>
      validationErrors?.containsKey(field) ?? false;
}
