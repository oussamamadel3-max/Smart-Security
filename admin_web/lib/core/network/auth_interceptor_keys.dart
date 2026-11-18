/// Dio [Options.extra] keys for the web AuthInterceptor / CSRF attach.
abstract final class AuthInterceptorKeys {
  static const skipAuthRefresh = 'skipAuthRefresh';
  static const retriedAfterRefresh = 'retriedAfterRefresh';
  static const attachCsrf = 'attachCsrf';
}
