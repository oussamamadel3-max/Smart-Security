import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';

import '../config/app_core_config.dart';
import 'interceptors/error_interceptor.dart';
import 'interceptors/log_interceptor.dart';

/// Builds a base [Dio] with timeouts and [ErrorInterceptor] only.
///
/// Authentication / refresh interceptors belong in each host app (T-02).
abstract final class DioClientSetup {
  static Dio build({
    required AppCoreConfig config,
    List<Interceptor> extraInterceptors = const [],
  }) {
    final dio = Dio(
      BaseOptions(
        baseUrl: config.baseUrl,
        connectTimeout: config.connectTimeout,
        receiveTimeout: config.receiveTimeout,
        sendTimeout: config.sendTimeout,
        headers: config.defaultHeaders,
      ),
    );

    if (config.httpClientAdapter != null) {
      dio.httpClientAdapter = config.httpClientAdapter!;
    }

    dio.interceptors.addAll([
      ...extraInterceptors,
      if (kDebugMode) prettyLogInterceptor,
      ErrorInterceptor(),
    ]);

    return dio;
  }
}
