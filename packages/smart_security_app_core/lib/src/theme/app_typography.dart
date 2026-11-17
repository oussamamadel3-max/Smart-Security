import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

import 'app_colors.dart';
import 'design_tokens.dart';

/// Typography — Cairo (design/index.html), with offline-safe fallback.
abstract final class AppTypography {
  static String get fontFamily => DesignTokens.fontFamily;

  static TextTheme get textTheme {
    final base = _textTheme(
      display: AppColors.lightOnBackground,
      body: AppColors.lightOnSurface,
      muted: AppColors.lightSubtext,
      faint: AppColors.lightDisabled,
    );
    // Avoid network font fetch when disabled (tests / offline CI).
    if (!GoogleFonts.config.allowRuntimeFetching) {
      return base;
    }
    return GoogleFonts.cairoTextTheme(base);
  }

  static TextTheme _textTheme({
    required Color display,
    required Color body,
    required Color muted,
    required Color faint,
  }) {
    TextStyle style({
      required double size,
      FontWeight weight = FontWeight.w400,
      Color? color,
      double? height,
      double? letterSpacing,
    }) {
      return TextStyle(
        fontFamily: fontFamily,
        fontFamilyFallback: const ['Tahoma', 'Arial', 'sans-serif'],
        fontSize: size,
        fontWeight: weight,
        color: color,
        height: height,
        letterSpacing: letterSpacing,
      );
    }

    return TextTheme(
      displayLarge: style(size: 40, weight: FontWeight.w800, color: display),
      displayMedium: style(size: 32, weight: FontWeight.w700, color: display),
      displaySmall: style(size: 28, weight: FontWeight.w700, color: display),
      headlineLarge: style(size: 25, weight: FontWeight.w800, color: display),
      headlineMedium: style(size: 22, weight: FontWeight.w700, color: display),
      headlineSmall: style(size: 19, weight: FontWeight.w700, color: display),
      titleLarge: style(size: 16, weight: FontWeight.w700, color: display),
      titleMedium: style(size: 14, weight: FontWeight.w700, color: body),
      titleSmall: style(size: 13, weight: FontWeight.w600, color: body),
      bodyLarge:
          style(size: 14, weight: FontWeight.w500, color: body, height: 1.5),
      bodyMedium:
          style(size: 13, weight: FontWeight.w400, color: body, height: 1.5),
      bodySmall:
          style(size: 12, weight: FontWeight.w400, color: muted, height: 1.5),
      labelLarge: style(size: 12, weight: FontWeight.w700, color: body),
      labelMedium: style(size: 11, weight: FontWeight.w600, color: muted),
      labelSmall: style(size: 10, weight: FontWeight.w600, color: faint),
    );
  }
}
