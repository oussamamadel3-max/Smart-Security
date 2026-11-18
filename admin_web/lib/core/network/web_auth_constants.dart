/// Cookie / CSRF names aligned with Backend `AuthOptions` defaults.
abstract final class WebAuthConstants {
  static const refreshDeliveryCookie = 'cookie';
  static const refreshChannelCookie = 'COOKIE';

  /// HttpOnly refresh cookie — browser-managed; never readable from JS.
  static const refreshCookieName = 'esp32_rt';

  /// Non-HttpOnly CSRF cookie — readable for double-submit header.
  static const csrfCookieName = 'esp32_csrf';
  static const csrfHeaderName = 'X-CSRF-TOKEN';
}
