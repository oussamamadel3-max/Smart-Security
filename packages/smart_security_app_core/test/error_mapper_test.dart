import 'package:flutter_test/flutter_test.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

void main() {
  group('ErrorMapper', () {
    test('401 becomes UnauthorizedFailure with mapped code message', () {
      final failure = ErrorMapper.map(
        const HttpException(
          statusCode: 401,
          errorCode: ApiErrorCodes.authUnauthenticated,
          message: 'raw server detail must not drive UI',
        ),
      );
      expect(failure, isA<UnauthorizedFailure>());
      expect(failure.displayMessage, isNot(contains('raw server')));
      expect(failure.requiresReauthentication, isTrue);
    });

    test('403 becomes ForbiddenFailure', () {
      final failure = ErrorMapper.map(
        const HttpException(
          statusCode: 403,
          errorCode: ApiErrorCodes.authForbidden,
        ),
      );
      expect(failure, isA<ForbiddenFailure>());
    });

    test('network exception is retryable', () {
      final failure = ErrorMapper.map(const NetworkException());
      expect(failure.isRetryable, isTrue);
    });
  });
}
