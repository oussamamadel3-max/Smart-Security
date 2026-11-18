import 'package:dio/dio.dart';

import '../session/csrf_token_reader.dart';
import 'auth_interceptor_keys.dart';
import 'web_auth_constants.dart';

/// Attaches `X-CSRF-TOKEN` when [AuthInterceptorKeys.attachCsrf] is set.
///
/// Does not navigate and never logs the token value.
class CsrfHeaderInterceptor extends Interceptor {
  CsrfHeaderInterceptor({required CsrfTokenReader csrfTokenReader})
    : _csrf = csrfTokenReader;

  final CsrfTokenReader _csrf;

  @override
  void onRequest(RequestOptions options, RequestInterceptorHandler handler) {
    if (options.extra[AuthInterceptorKeys.attachCsrf] == true) {
      final token = _csrf.read();
      if (token != null && token.isNotEmpty) {
        options.headers[WebAuthConstants.csrfHeaderName] = token;
      }
    }
    handler.next(options);
  }
}
