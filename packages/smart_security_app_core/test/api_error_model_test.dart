import 'package:flutter_test/flutter_test.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

void main() {
  test('parses Backend ApiResponse.Error envelope', () {
    final model = ApiErrorModel.fromJson({
      'success': false,
      'error': {
        'code': 'AUTH_UNAUTHENTICATED',
        'message': 'server detail',
        'meta': <String, dynamic>{},
      },
    });

    expect(model.success, isFalse);
    expect(model.errorCode, 'AUTH_UNAUTHENTICATED');
    expect(model.serverMessage, 'server detail');
  });
}
