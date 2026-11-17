import 'package:pretty_dio_logger/pretty_dio_logger.dart';

/// Debug-only HTTP logger. Bodies disabled to avoid leaking tokens.
final prettyLogInterceptor = PrettyDioLogger(
  requestHeader: false,
  requestBody: false,
  responseBody: false,
  responseHeader: false,
  compact: true,
);
