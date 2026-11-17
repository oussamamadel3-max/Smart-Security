import 'package:dio/dio.dart';

import '../../errors/exceptions/api_exception.dart';
import '../../errors/models/api_error_model.dart';

/// Maps Dio failures to [ApiException] using the Backend error envelope.
class ErrorInterceptor extends Interceptor {
  @override
  void onError(DioException err, ErrorInterceptorHandler handler) {
    final mapped = _mapDioException(err);
    handler.next(err.copyWith(error: mapped));
  }

  ApiException _mapDioException(DioException err) {
    switch (err.type) {
      case DioExceptionType.connectionError:
        return const NetworkException();
      case DioExceptionType.connectionTimeout:
      case DioExceptionType.sendTimeout:
      case DioExceptionType.receiveTimeout:
        return const TimeoutException();
      case DioExceptionType.badResponse:
        return _mapBadResponse(err);
      case DioExceptionType.cancel:
        return UnexpectedException(exception: err.error ?? err);
      case DioExceptionType.unknown:
        if (_looksLikeNetwork(err)) {
          return const NetworkException();
        }
        return UnexpectedException(exception: err);
      default:
        return UnexpectedException(exception: err);
    }
  }

  ApiException _mapBadResponse(DioException err) {
    final statusCode = err.response?.statusCode ?? 0;

    try {
      final data = err.response?.data;
      if (data is Map) {
        final model = ApiErrorModel.fromJson(Map<String, dynamic>.from(data));
        return HttpException(
          statusCode: statusCode,
          errorCode: model.errorCode,
          // Keep server message for logging only (HttpException.message).
          message: model.serverMessage,
          rawBody: data.toString(),
        );
      }
      return HttpException(statusCode: statusCode);
    } catch (_) {
      return HttpException(statusCode: statusCode);
    }
  }

  bool _looksLikeNetwork(DioException err) {
    final blob = '${err.error ?? ''} ${err.message ?? ''}'.toLowerCase();
    return blob.contains('socketexception') ||
        blob.contains('network is unreachable') ||
        blob.contains('connection refused') ||
        blob.contains('failed host lookup') ||
        blob.contains('connection reset') ||
        blob.contains('software caused connection abort');
  }
}
