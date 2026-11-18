import 'dart:convert';

/// Reads JWT payload claims without verifying the signature.
///
/// Client role checks are UX-only — Backend remains the authority.
abstract final class JwtClaimsReader {
  static const _roleClaim =
      'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
  static const _roleShort = 'role';
  static const _nameIdClaim =
      'http://schemas.microsoft.com/ws/2008/06/identity/claims/nameidentifier';

  static Map<String, dynamic>? decodePayload(String jwt) {
    final parts = jwt.split('.');
    if (parts.length < 2) return null;
    try {
      final normalized = base64Url.normalize(parts[1]);
      final json = utf8.decode(base64Url.decode(normalized));
      final decoded = jsonDecode(json);
      if (decoded is Map<String, dynamic>) return decoded;
      if (decoded is Map) return Map<String, dynamic>.from(decoded);
      return null;
    } catch (_) {
      return null;
    }
  }

  static List<String> roles(String jwt) {
    final payload = decodePayload(jwt);
    if (payload == null) return const [];

    final collected = <String>{};
    void add(dynamic value) {
      if (value is String && value.trim().isNotEmpty) {
        collected.add(value.trim());
      } else if (value is List) {
        for (final item in value) {
          if (item is String && item.trim().isNotEmpty) {
            collected.add(item.trim());
          }
        }
      }
    }

    add(payload[_roleClaim]);
    add(payload[_roleShort]);
    return collected.toList(growable: false);
  }

  static String? subject(String jwt) {
    final payload = decodePayload(jwt);
    if (payload == null) return null;
    final sub = payload['sub'] ?? payload[_nameIdClaim];
    if (sub is String && sub.trim().isNotEmpty) return sub.trim();
    return null;
  }

  static bool hasRole(String jwt, String roleCode) {
    final target = roleCode.trim().toUpperCase();
    return roles(jwt).any((r) => r.toUpperCase() == target);
  }
}
