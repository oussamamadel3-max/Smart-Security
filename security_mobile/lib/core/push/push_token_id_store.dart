import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// Persists only the server `pushTokenId` — never the raw FCM token.
class PushTokenIdStore {
  PushTokenIdStore(this._storage);

  static const _key = 'hisn_push_token_id';

  final FlutterSecureStorage _storage;

  Future<String?> read() => _storage.read(key: _key);

  Future<void> write(String pushTokenId) =>
      _storage.write(key: _key, value: pushTokenId);

  Future<void> clear() => _storage.delete(key: _key);
}
