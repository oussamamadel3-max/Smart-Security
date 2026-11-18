import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:security_mobile/core/session/jwt_claims_reader.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

String _jwt(Map<String, dynamic> payload) {
  String enc(Object value) =>
      base64Url.encode(utf8.encode(value is String ? value : jsonEncode(value)))
          .replaceAll('=', '');
  return '${enc('{"alg":"none"}')}.${enc(payload)}.sig';
}

void main() {
  test('reads OWNER role from long ClaimTypes.Role URI', () {
    final token = _jwt({
      'sub': 'user-1',
      'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': 'OWNER',
    });
    expect(JwtClaimsReader.hasRole(token, AppRoleCodes.owner), isTrue);
    expect(JwtClaimsReader.subject(token), 'user-1');
  });

  test('reads role short claim list', () {
    final token = _jwt({
      'sub': 'user-2',
      'role': ['ADMIN', 'OWNER'],
    });
    expect(JwtClaimsReader.roles(token), containsAll(['ADMIN', 'OWNER']));
  });
}
