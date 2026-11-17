/// Verified Backend API error codes (from `Domain.Exceptions` / Auth).
///
/// User-facing messages are owned by the client — never display raw Backend
/// `error.message` (may contain technical detail).
///
/// Session revoke *reasons* (LOGOUT, USER_DISABLED, …) are not HTTP error
/// codes; see [SessionRevokeReasons].
abstract final class ApiErrorCodes {
  // Auth / authorization (HTTP API)
  static const authUnauthenticated = 'AUTH_UNAUTHENTICATED';
  static const authForbidden = 'AUTH_FORBIDDEN';
  static const authChannelInvalid = 'AUTH_CHANNEL_INVALID';

  // Generic business / HTTP
  static const badRequest = 'BAD_REQUEST';
  static const notFound = 'NOT_FOUND';
  static const conflict = 'CONFLICT';
  static const serviceUnavailable = 'SERVICE_UNAVAILABLE';

  static const Map<String, String> _messages = {
    authUnauthenticated: 'انتهت جلستك، يرجى تسجيل الدخول مجدداً',
    authForbidden: 'ليس لديك صلاحية لإجراء هذه العملية',
    authChannelInvalid: 'قناة التحديث غير مدعومة',
    badRequest: 'الطلب غير صالح',
    notFound: 'العنصر المطلوب غير موجود',
    conflict: 'تعارض في البيانات',
    serviceUnavailable: 'الخدمة غير متاحة مؤقتاً، يرجى المحاولة لاحقاً',
  };

  /// Safe Arabic (or fallback) message for a Backend [code].
  static String getMessage(String? code, {String? fallback}) {
    if (code == null || code.isEmpty) {
      return fallback ?? 'حدث خطأ غير متوقع';
    }
    return _messages[code] ?? fallback ?? 'حدث خطأ غير متوقع';
  }

  static bool requiresReauth(String? code) =>
      code == authUnauthenticated;
}

/// Server-side session revoke reasons (`AuthStatusCodes.RevokeReason`).
/// Not shown as API `error.code` for normal UI mapping.
abstract final class SessionRevokeReasons {
  static const logout = 'LOGOUT';
  static const logoutAll = 'LOGOUT_ALL';
  static const refreshReuse = 'REFRESH_REUSE';
  static const userDisabled = 'USER_DISABLED';
  static const userArchived = 'USER_ARCHIVED';
  static const absoluteExpired = 'ABSOLUTE_EXPIRED';
  static const replaced = 'REPLACED';
}
