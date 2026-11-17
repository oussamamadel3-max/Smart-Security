import 'package:flutter/material.dart';

/// Design tokens extracted from `design/index.html` (حِصن).
///
/// Single light visual language — no dark theme in v1.
abstract final class DesignTokens {
  // ── Colors (:root) ─────────────────────────────────────────
  static const navy = Color(0xFF10233C);
  static const navy2 = Color(0xFF172F4B);
  static const primary = Color(0xFF176CBA);
  static const sky = Color(0xFFEAF5FF);
  static const background = Color(0xFFF4F7FB);
  static const card = Color(0xFFFFFFFF);
  static const ink = Color(0xFF182B40);
  static const muted = Color(0xFF79899B);
  static const border = Color(0xFFE7EDF4);
  static const success = Color(0xFF1B9B76);
  static const danger = Color(0xFFDD5461);
  static const warning = Color(0xFFD99927);

  // Derived / component accents from the same HTML prototype
  static const sidebarText = Color(0xFFCAD9E9);
  static const brandMarkStart = Color(0xFF29BAD4);
  static const brandMarkEnd = Color(0xFF3479D9);
  static const focusRing = Color(0xFF78B8F5);
  static const primarySoft = Color(0xFFEAF2FC);
  static const primaryTextSoft = Color(0xFF1763AD);
  static const outlineButton = Color(0xFF38516E);
  static const dangerSoftBg = Color(0xFFFFF0F1);
  static const dangerSoftText = Color(0xFFC73948);
  static const successSoftBg = Color(0xFFE6F8F1);
  static const successSoftText = Color(0xFF168666);
  static const warningSoftBg = Color(0xFFFFF5E3);
  static const warningSoftText = Color(0xFFB67B1D);
  static const infoSoftBg = Color(0xFFE9F3FF);
  static const infoSoftText = Color(0xFF286BB1);
  static const noticeBg = Color(0xFFFFF8E9);
  static const noticeText = Color(0xFF9D6818);
  static const noticeBorder = Color(0xFFF4E1B8);
  static const toastBg = Color(0xFF152E4B);
  static const white = Color(0xFFFFFFFF);
  static const black = Color(0xFF000000);
  static const transparent = Colors.transparent;

  // ── Radii ──────────────────────────────────────────────────
  static const radiusSm = 8.0;
  static const radiusMd = 10.0;
  static const radiusLg = 13.0;
  static const radiusXl = 16.0;
  static const radiusModal = 18.0;
  static const radiusPill = 20.0;

  // ── Spacing ────────────────────────────────────────────────
  static const spaceXs = 4.0;
  static const spaceSm = 8.0;
  static const spaceMd = 12.0;
  static const spaceLg = 16.0;
  static const spaceXl = 24.0;
  static const spaceXxl = 34.0;

  // ── Elevation / shadow ─────────────────────────────────────
  static List<BoxShadow> get cardShadow => [
        BoxShadow(
          color: const Color(0xFF203855).withValues(alpha: 0.055),
          blurRadius: 28,
          offset: const Offset(0, 8),
        ),
      ];

  static const fontFamily = 'Cairo';
}
