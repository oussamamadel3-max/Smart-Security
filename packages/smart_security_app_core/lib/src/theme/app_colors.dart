import 'package:flutter/material.dart';

import 'design_tokens.dart';

/// Application color aliases mapped from [DesignTokens] (light only).
abstract final class AppColors {
  static const primary50 = DesignTokens.sky;
  static const primary100 = Color(0xFFD9ECFF);
  static const primary200 = Color(0xFFB3D9FF);
  static const primary300 = Color(0xFF7CBCF0);
  static const primary400 = Color(0xFF4A9BDD);
  static const primary500 = DesignTokens.primary;
  static const primary600 = Color(0xFF145FA3);
  static const primary700 = Color(0xFF114E87);
  static const primary800 = DesignTokens.navy2;
  static const primary900 = DesignTokens.navy;
  static const primary950 = Color(0xFF0A1626);

  static const lightBackground = DesignTokens.background;
  static const lightSurface = DesignTokens.card;
  static const lightSurfaceVariant = Color(0xFFF7F9FC);
  static const lightOnBackground = DesignTokens.ink;
  static const lightOnSurface = DesignTokens.ink;
  static const lightSubtext = DesignTokens.muted;
  static const lightDisabled = Color(0xFFB0BBC8);
  static const lightBorder = DesignTokens.border;
  static const lightBorderSubtle = Color(0xFFEDF2F7);
  static const lightDivider = Color(0xFFEDF1F5);

  static const success = DesignTokens.success;
  static const warning = DesignTokens.warning;
  static const error = DesignTokens.danger;
  static const info = DesignTokens.primary;
  static const infoBg = DesignTokens.sky;

  static const white = DesignTokens.white;
  static const black = DesignTokens.black;
  static const transparent = DesignTokens.transparent;

  static const navy = DesignTokens.navy;
  static const toast = DesignTokens.toastBg;
}
