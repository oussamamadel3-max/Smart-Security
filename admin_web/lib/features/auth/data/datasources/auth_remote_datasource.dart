import 'package:dio/dio.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/network/app_endpoints.dart';
import '../../../../core/network/auth_interceptor_keys.dart';
import '../../../../core/network/web_auth_constants.dart';
import '../../../../core/session/access_token_holder.dart';
import '../models/auth_tokens_dto.dart';
import '../models/me_user_dto.dart';

/// Auth HTTP calls on a bare Dio (no AuthInterceptor) to avoid refresh loops.
///
/// Cookie transport relies on browser credentials; CSRF attaches via extra flag.
class AuthRemoteDataSource {
  AuthRemoteDataSource({
    required Dio bareDio,
    required AccessTokenHolder accessTokenHolder,
  }) : _dio = bareDio,
       _access = accessTokenHolder;

  final Dio _dio;
  final AccessTokenHolder _access;

  Future<Result<AuthTokensDto>> login({
    required String userName,
    required String password,
  }) {
    return _wrap(
      () => _dio.post(
        AppEndpoints.login,
        data: {
          'userName': userName,
          'password': password,
          'refreshDelivery': WebAuthConstants.refreshDeliveryCookie,
        },
        options: Options(
          extra: const {AuthInterceptorKeys.skipAuthRefresh: true},
        ),
      ),
      (json) => AuthTokensDto.fromJson(_asMap(json)),
    );
  }

  Future<Result<AuthTokensDto>> refresh() {
    return _wrap(
      () => _dio.post(
        AppEndpoints.refresh,
        data: <String, dynamic>{},
        options: Options(
          extra: const {
            AuthInterceptorKeys.skipAuthRefresh: true,
            AuthInterceptorKeys.attachCsrf: true,
          },
        ),
      ),
      (json) => AuthTokensDto.fromJson(_asMap(json)),
    );
  }

  Future<Result<MeUserDto>> me() {
    return _wrap(
      () => _dio.get(
        AppEndpoints.me,
        options: Options(headers: _bearerHeaders()),
      ),
      (json) => MeUserDto.fromJson(_asMap(json)),
    );
  }

  Future<Result<bool>> logout() {
    return _wrap(
      () => _dio.post(
        AppEndpoints.logout,
        data: <String, dynamic>{},
        options: Options(
          headers: _bearerHeaders(),
          extra: const {AuthInterceptorKeys.attachCsrf: true},
        ),
      ),
      (_) => true,
    );
  }

  Future<Result<bool>> logoutAll() {
    return _wrap(
      () => _dio.post(
        AppEndpoints.logoutAll,
        options: Options(headers: _bearerHeaders()),
      ),
      (_) => true,
    );
  }

  Map<String, dynamic> _bearerHeaders() {
    final token = _access.accessToken;
    if (token == null || token.isEmpty) return const {};
    return {'Authorization': 'Bearer $token'};
  }

  Future<Result<T>> _wrap<T>(
    Future<Response<dynamic>> Function() request,
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

  Map<String, dynamic> _asMap(dynamic json) {
    if (json is Map<String, dynamic>) return json;
    if (json is Map) return Map<String, dynamic>.from(json);
    throw const FormatException('Expected JSON object');
  }
}
