import 'package:flutter_test/flutter_test.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

void main() {
  test('required rejects empty', () {
    expect(AppValidators.required(''), isNotNull);
    expect(AppValidators.required('x'), isNull);
  });

  test('email validates format when present', () {
    expect(AppValidators.email(null), isNull);
    expect(AppValidators.email('a@b.com'), isNull);
    expect(AppValidators.email('bad'), isNotNull);
  });
}
