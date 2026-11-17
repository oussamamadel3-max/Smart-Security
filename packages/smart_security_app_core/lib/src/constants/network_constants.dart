/// Default HTTP client settings shared by [AppCoreConfig] and [DioClientSetup].
abstract final class NetworkConstants {
  static const connectTimeout = Duration(seconds: 15);
  static const receiveTimeout = Duration(seconds: 30);
  static const sendTimeout = Duration(seconds: 15);

  static const contentTypeJson = 'application/json';

  static const defaultHeaders = {
    'Content-Type': contentTypeJson,
    'Accept': contentTypeJson,
  };

  static const authorizationHeader = 'Authorization';
  static const bearerPrefix = 'Bearer';
}
