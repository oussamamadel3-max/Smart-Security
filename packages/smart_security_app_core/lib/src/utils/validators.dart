/// Lightweight shared validators (no feature-specific rules).
abstract final class AppValidators {
  static String? required(String? value, {String message = 'هذا الحقل إلزامي'}) {
    if (value == null || value.trim().isEmpty) return message;
    return null;
  }

  static String? minLength(
    String? value,
    int min, {
    String? message,
  }) {
    if (value == null || value.length < min) {
      return message ?? 'يجب ألا يقل عن $min أحرف';
    }
    return null;
  }

  static String? email(String? value, {String message = 'البريد غير صالح'}) {
    if (value == null || value.trim().isEmpty) return null;
    final ok = RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$').hasMatch(value.trim());
    return ok ? null : message;
  }
}
