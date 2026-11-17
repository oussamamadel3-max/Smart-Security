import 'package:dio/dio.dart';

import '../constants/auth_constants.dart';
import '../errors/exceptions/api_exception.dart';
import '../errors/mappers/error_mapper.dart';
import '../errors/result/result.dart';

abstract class ApiClient {
  final Dio dio;

  ApiClient(this.dio);

  Future<Result<T>> get<T>(
    String path, {
    Map<String, dynamic>? queryParameters,
    required T Function(dynamic json) parser,
  }) => _wrap(() => dio.get(path, queryParameters: queryParameters), parser);

  Future<Result<T>> post<T>(
    String path, {
    dynamic data,
    required T Function(dynamic json) parser,
  }) => _wrap(() => dio.post(path, data: data), parser);

  Future<Result<T>> put<T>(
    String path, {
    dynamic data,
    required T Function(dynamic json) parser,
  }) => _wrap(() => dio.put(path, data: data), parser);

  Future<Result<T>> delete<T>(
    String path, {
    dynamic data,
    required T Function(dynamic json) parser,
  }) => _wrap(() => dio.delete(path, data: data), parser);

  Future<Result<T>> _wrap<T>(
    Future<Response> Function() request,
    T Function(dynamic json) parser,
  ) async {
    try {
      final response = await request();
      final raw = response.data;
      final data = (raw is Map && raw.containsKey(ApiEnvelopeKeys.data))
          ? raw[ApiEnvelopeKeys.data]
          : raw;
      return Success(parser(data));
    } on DioException catch (e) {
      final apiException = e.error;
      if (apiException is ApiException) {
        return Failure_(ErrorMapper.map(apiException));
      }
      return Failure_(
        ErrorMapper.map(
          UnexpectedException(exception: e, stackTrace: e.stackTrace),
        ),
      );
    } catch (e, stackTrace) {
      return Failure_(
        ErrorMapper.map(
          ParseException(details: e.toString(), stackTrace: stackTrace),
        ),
      );
    }
  }
}
