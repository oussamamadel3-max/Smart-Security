/// Common API envelope keys from Backend `ApiResponse`.
abstract final class ApiEnvelopeKeys {
  static const data = 'data';
  static const success = 'success';
  static const message = 'message';
  static const error = 'error';
  static const meta = 'meta';
}

/// JSON field names observed on Auth token payloads (Mobile body channel).
/// Web Cookie refresh does not use these body keys the same way.
abstract final class TokenJsonKeys {
  static const refreshTokenRequest = 'refreshToken';
  static const accessTokenResponse = 'accessToken';
  static const refreshTokenResponse = 'refreshToken';
}
