import 'dart:convert';

import 'package:admin_web/core/session/jwt_claims_reader.dart';
import 'package:flutter_test/flutter_test.dart';

String _jwt(Map<String, dynamic> payload) {
  final header = base64Url
      .encode(utf8.encode('{"alg":"none"}'))
      .replaceAll('=', '');
  final body = base64Url
      .encode(utf8.encode(jsonEncode(payload)))
      .replaceAll('=', '');
  return '$header.$body.sig';
}

void main() {
  test('reads role claim list', () {
    final token = _jwt({
      'role': ['OWNER', 'ADMIN'],
    });
    expect(JwtClaimsReader.roles(token), containsAll(['OWNER', 'ADMIN']));
  });

  test('hasRole is case-insensitive', () {
    final token = _jwt({'role': 'owner'});
    expect(JwtClaimsReader.hasRole(token, 'OWNER'), isTrue);
  });
}
