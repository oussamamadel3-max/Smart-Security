import 'package:flutter_test/flutter_test.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

void main() {
  group('ApiErrorCodes', () {
    test('maps verified Backend auth codes to Arabic messages', () {
      expect(
        ApiErrorCodes.getMessage(ApiErrorCodes.authUnauthenticated),
        contains('جلستك'),
      );
      expect(
        ApiErrorCodes.getMessage(ApiErrorCodes.authForbidden),
        contains('صلاحية'),
      );
    });


    test('unknown codes use safe fallback without echoing secrets', () {
      expect(
        ApiErrorCodes.getMessage('SOMETHING_UNKNOWN'),
        'حدث خطأ غير متوقع',
      );
      expect(
        ApiErrorCodes.getMessage(null, fallback: 'fallback'),
        'fallback',
      );
    });

    test('does not treat LICENSE template codes as known', () {
      expect(ApiErrorCodes.getMessage('LICENSE_EXPIRED'), 'حدث خطأ غير متوقع');
    });
  });

  group('SessionRevokeReasons', () {
    test('exposes Backend revoke reasons separately from API codes', () {
      expect(SessionRevokeReasons.userDisabled, 'USER_DISABLED');
      expect(SessionRevokeReasons.refreshReuse, 'REFRESH_REUSE');
    });
  });
}
