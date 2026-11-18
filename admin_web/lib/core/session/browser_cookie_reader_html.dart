// ignore_for_file: avoid_web_libraries_in_flutter, deprecated_member_use
import 'dart:html' as html;

/// Reads a non-HttpOnly cookie from `document.cookie`.
String? readBrowserCookie(String name) {
  final all = html.document.cookie ?? '';
  if (all.isEmpty) return null;
  for (final part in all.split(';')) {
    final idx = part.indexOf('=');
    if (idx <= 0) continue;
    final key = part.substring(0, idx).trim();
    if (key != name) continue;
    final raw = part.substring(idx + 1).trim();
    try {
      return Uri.decodeComponent(raw);
    } catch (_) {
      return raw;
    }
  }
  return null;
}
