/// In-memory holder for the short-lived Access JWT.
///
/// Never persist the access token — Refresh lives in secure storage only.
class AccessTokenHolder {
  String? _accessToken;

  String? get accessToken => _accessToken;

  bool get hasAccessToken =>
      _accessToken != null && _accessToken!.trim().isNotEmpty;

  void setToken(String? token) {
    final trimmed = token?.trim();
    _accessToken = (trimmed == null || trimmed.isEmpty) ? null : trimmed;
  }

  void clear() => _accessToken = null;
}
