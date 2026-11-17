import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import 'app_colors.dart';
import 'app_semantic_colors.dart';
import 'app_typography.dart';
import 'design_tokens.dart';

/// Light-only [ThemeData] aligned with `design/index.html`.
abstract final class AppTheme {
  static ThemeData get light => ThemeData(
        useMaterial3: true,
        brightness: Brightness.light,
        fontFamily: AppTypography.fontFamily,
        textTheme: AppTypography.textTheme,
        extensions: const [AppSemanticColors.light],
        colorScheme: const ColorScheme.light(
          primary: AppColors.primary500,
          onPrimary: AppColors.white,
          primaryContainer: AppColors.primary100,
          onPrimaryContainer: AppColors.primary900,
          secondary: AppColors.primary600,
          onSecondary: AppColors.white,
          secondaryContainer: AppColors.primary50,
          onSecondaryContainer: AppColors.primary800,
          tertiary: AppColors.primary400,
          onTertiary: AppColors.white,
          error: AppColors.error,
          onError: AppColors.white,
          errorContainer: DesignTokens.dangerSoftBg,
          onErrorContainer: DesignTokens.dangerSoftText,
          surface: AppColors.lightSurface,
          onSurface: AppColors.lightOnSurface,
          surfaceContainerHighest: AppColors.lightSurfaceVariant,
          onSurfaceVariant: AppColors.lightSubtext,
          outline: AppColors.lightBorder,
          outlineVariant: AppColors.lightBorderSubtle,
          shadow: AppColors.black,
          scrim: Color(0x99102137),
          inverseSurface: AppColors.primary900,
          onInverseSurface: AppColors.white,
          inversePrimary: AppColors.primary300,
        ),
        scaffoldBackgroundColor: AppColors.lightBackground,
        appBarTheme: AppBarTheme(
          backgroundColor: AppColors.lightSurface,
          foregroundColor: AppColors.lightOnBackground,
          elevation: 0,
          scrolledUnderElevation: 1,
          shadowColor: AppColors.lightBorder,
          centerTitle: true,
          systemOverlayStyle: SystemUiOverlayStyle.dark,
          titleTextStyle: AppTypography.textTheme.titleLarge,
          iconTheme: const IconThemeData(
            color: AppColors.lightOnBackground,
            size: 24,
          ),
        ),
        cardTheme: CardThemeData(
          color: AppColors.lightSurface,
          elevation: 0,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(DesignTokens.radiusXl),
            side: const BorderSide(color: AppColors.lightBorder),
          ),
          margin: EdgeInsets.zero,
        ),
        elevatedButtonTheme: ElevatedButtonThemeData(
          style: ElevatedButton.styleFrom(
            backgroundColor: AppColors.primary500,
            foregroundColor: AppColors.white,
            disabledBackgroundColor: AppColors.lightDisabled,
            disabledForegroundColor: AppColors.white,
            elevation: 0,
            padding: const EdgeInsets.symmetric(horizontal: 17, vertical: 10),
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(DesignTokens.radiusMd),
            ),
            textStyle: AppTypography.textTheme.labelLarge,
          ),
        ),
        outlinedButtonTheme: OutlinedButtonThemeData(
          style: OutlinedButton.styleFrom(
            foregroundColor: DesignTokens.outlineButton,
            side: const BorderSide(color: Color(0xFFDCE7F2)),
            padding: const EdgeInsets.symmetric(horizontal: 17, vertical: 10),
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(DesignTokens.radiusMd),
            ),
            textStyle: AppTypography.textTheme.labelLarge,
          ),
        ),
        textButtonTheme: TextButtonThemeData(
          style: TextButton.styleFrom(
            foregroundColor: AppColors.primary500,
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(DesignTokens.radiusMd),
            ),
            textStyle: AppTypography.textTheme.labelLarge,
          ),
        ),
        inputDecorationTheme: InputDecorationTheme(
          filled: true,
          fillColor: AppColors.white,
          contentPadding:
              const EdgeInsets.symmetric(horizontal: 13, vertical: 12),
          border: OutlineInputBorder(
            borderRadius: BorderRadius.circular(DesignTokens.radiusMd),
            borderSide: const BorderSide(color: Color(0xFFDFE7EF)),
          ),
          enabledBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(DesignTokens.radiusMd),
            borderSide: const BorderSide(color: Color(0xFFDFE7EF)),
          ),
          focusedBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(DesignTokens.radiusMd),
            borderSide: const BorderSide(color: AppColors.primary500, width: 2),
          ),
          errorBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(DesignTokens.radiusMd),
            borderSide: const BorderSide(color: AppColors.error),
          ),
          focusedErrorBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(DesignTokens.radiusMd),
            borderSide: const BorderSide(color: AppColors.error, width: 2),
          ),
          hintStyle: AppTypography.textTheme.bodyMedium?.copyWith(
            color: AppColors.lightDisabled,
          ),
          labelStyle: AppTypography.textTheme.labelMedium,
        ),
        dialogTheme: DialogThemeData(
          backgroundColor: AppColors.lightSurface,
          elevation: 8,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(DesignTokens.radiusModal),
          ),
          titleTextStyle: AppTypography.textTheme.headlineSmall,
          contentTextStyle: AppTypography.textTheme.bodyMedium,
        ),
        snackBarTheme: SnackBarThemeData(
          backgroundColor: AppColors.toast,
          contentTextStyle: AppTypography.textTheme.bodyMedium?.copyWith(
            color: AppColors.white,
          ),
          behavior: SnackBarBehavior.floating,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(DesignTokens.radiusLg),
          ),
        ),
        dividerTheme: const DividerThemeData(
          color: AppColors.lightDivider,
          thickness: 1,
          space: 1,
        ),
        progressIndicatorTheme: const ProgressIndicatorThemeData(
          color: AppColors.primary500,
          linearTrackColor: AppColors.primary100,
          circularTrackColor: AppColors.primary100,
        ),
        chipTheme: ChipThemeData(
          backgroundColor: DesignTokens.infoSoftBg,
          selectedColor: AppColors.primary100,
          labelStyle: AppTypography.textTheme.labelMedium?.copyWith(
            color: DesignTokens.infoSoftText,
          ),
          side: BorderSide.none,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(DesignTokens.radiusPill),
          ),
          padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 5),
        ),
      );
}
