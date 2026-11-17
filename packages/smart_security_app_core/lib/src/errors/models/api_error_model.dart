/// Matches Backend `ApiResponse.Error` JSON:
/// `{ "success": false, "error": { "code", "message", "meta" } }`.
class ApiErrorModel {
  const ApiErrorModel({
    this.success = false,
    this.error,
  });

  final bool success;
  final ApiErrorBody? error;

  factory ApiErrorModel.fromJson(Map<String, dynamic> json) {
    final rawError = json['error'];
    return ApiErrorModel(
      success: json['success'] as bool? ?? false,
      error: rawError is Map
          ? ApiErrorBody.fromJson(Map<String, dynamic>.from(rawError))
          : null,
    );
  }
}

class ApiErrorBody {
  const ApiErrorBody({
    this.code,
    this.message,
    this.meta = const {},
  });

  final String? code;
  final String? message;
  final Map<String, dynamic> meta;

  factory ApiErrorBody.fromJson(Map<String, dynamic> json) {
    final rawMeta = json['meta'];
    return ApiErrorBody(
      code: json['code'] as String?,
      message: json['message'] as String?,
      meta: rawMeta is Map
          ? Map<String, dynamic>.from(rawMeta)
          : const {},
    );
  }
}

extension ApiErrorModelX on ApiErrorModel {
  String? get errorCode => error?.code;

  /// Backend message is for logging only — never show raw to the user.
  String? get serverMessage => error?.message;
}
