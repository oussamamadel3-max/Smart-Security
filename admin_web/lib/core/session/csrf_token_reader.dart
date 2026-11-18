import '../network/web_auth_constants.dart';
import 'browser_cookie_reader.dart';

/// Reads the non-HttpOnly CSRF cookie for double-submit header attachment.
abstract class CsrfTokenReader {
  String? read();
}

class BrowserCsrfTokenReader implements CsrfTokenReader {
  @override
  String? read() => readBrowserCookie(WebAuthConstants.csrfCookieName);
}

/// Test / VM injectable reader.
class MemoryCsrfTokenReader implements CsrfTokenReader {
  MemoryCsrfTokenReader([this.value]);

  String? value;

  @override
  String? read() => value;
}
