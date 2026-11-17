import 'package:flutter/material.dart';

import 'app_colors.dart';
import 'design_tokens.dart';

/// Semantic colors that [ColorScheme] does not expose (success / warning).
@immutable
class AppSemanticColors extends ThemeExtension<AppSemanticColors> {
  const AppSemanticColors({
    required this.success,
    required this.onSuccess,
    required this.warning,
    required this.onWarning,
  });

  final Color success;
  final Color onSuccess;
  final Color warning;
  final Color onWarning;

  static const light = AppSemanticColors(
    success: DesignTokens.success,
    onSuccess: AppColors.white,
    warning: DesignTokens.warning,
    onWarning: AppColors.white,
  );

  static AppSemanticColors of(BuildContext context) {
    final extension = Theme.of(context).extension<AppSemanticColors>();
    assert(
      extension != null,
      'AppSemanticColors is missing from ThemeData.extensions',
    );
    return extension!;
  }

  @override
  AppSemanticColors copyWith({
    Color? success,
    Color? onSuccess,
    Color? warning,
    Color? onWarning,
  }) {
    return AppSemanticColors(
      success: success ?? this.success,
      onSuccess: onSuccess ?? this.onSuccess,
      warning: warning ?? this.warning,
      onWarning: onWarning ?? this.onWarning,
    );
  }

  @override
  AppSemanticColors lerp(ThemeExtension<AppSemanticColors>? other, double t) {
    if (other is! AppSemanticColors) return this;
    return AppSemanticColors(
      success: Color.lerp(success, other.success, t)!,
      onSuccess: Color.lerp(onSuccess, other.onSuccess, t)!,
      warning: Color.lerp(warning, other.warning, t)!,
      onWarning: Color.lerp(onWarning, other.onWarning, t)!,
    );
  }
}
