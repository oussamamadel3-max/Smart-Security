/// Helpers for path templates such as `/api/owner/devices/:id`.
abstract final class EndpointPath {
  EndpointPath._();

  static const idPlaceholder = ':id';

  /// Replaces `:id` (and optional custom placeholders) in [template].
  static String withParams(
    String template, {
    String? id,
    Map<String, String> params = const {},
  }) {
    var result = template;
    if (id != null) {
      result = result.replaceAll(idPlaceholder, id);
    }
    for (final entry in params.entries) {
      result = result.replaceAll(':${entry.key}', entry.value);
    }
    return result;
  }

  /// `/api/{resource}`
  static String collection(String resource) => '/api/$resource';

  /// `/api/{resource}/:id`
  static String resource(String resource) =>
      '${collection(resource)}/$idPlaceholder';

  /// Nests [child] under a resolved [parent] path.
  static String nested(String parent, String child) {
    final normalizedParent = parent.endsWith('/') ? parent : '$parent/';
    final normalizedChild =
        child.startsWith('/') ? child.substring(1) : child;
    return '$normalizedParent$normalizedChild';
  }
}
