import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// Contract for persisting only the Refresh Token.
abstract class RefreshTokenStore {
  Future<String?> read();
  Future<void> write(String token);
  Future<void> clear();
}

/// Platform secure-storage implementation.
class SecureRefreshTokenStore implements RefreshTokenStore {
  SecureRefreshTokenStore(this._storage);

  static const _refreshKey = 'hisn.refresh_token.v1';

  final FlutterSecureStorage _storage;

  @override
  Future<String?> read() async {
    final value = await _storage.read(key: _refreshKey);
    if (value == null || value.trim().isEmpty) return null;
    return value;
  }

  @override
  Future<void> write(String token) async {
    final trimmed = token.trim();
    if (trimmed.isEmpty) {
      await clear();
      return;
    }
    await _storage.write(key: _refreshKey, value: trimmed);
  }

  @override
  Future<void> clear() => _storage.delete(key: _refreshKey);
}
