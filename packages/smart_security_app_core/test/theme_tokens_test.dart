import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

void main() {
  setUpAll(() {
    GoogleFonts.config.allowRuntimeFetching = false;
  });

  test('DesignTokens match design/index.html :root anchors', () {
    expect(DesignTokens.navy, const Color(0xFF10233C));
    expect(DesignTokens.primary, const Color(0xFF176CBA));
    expect(DesignTokens.background, const Color(0xFFF4F7FB));
    expect(DesignTokens.ink, const Color(0xFF182B40));
    expect(DesignTokens.success, const Color(0xFF1B9B76));
    expect(DesignTokens.danger, const Color(0xFFDD5461));
    expect(DesignTokens.warning, const Color(0xFFD99927));
  });

  test('AppTheme.light is light-only and uses primary from tokens', () {
    final theme = AppTheme.light;
    expect(theme.brightness, Brightness.light);
    expect(theme.colorScheme.primary, DesignTokens.primary);
    expect(theme.extensions[AppSemanticColors], isNotNull);
  });
}
