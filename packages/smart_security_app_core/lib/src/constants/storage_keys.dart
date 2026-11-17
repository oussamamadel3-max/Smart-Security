/// Keys for [SharedPreferences] (non-sensitive UI prefs only).
///
/// Access/refresh tokens must never use SharedPreferences — host apps own
/// secure storage (Mobile) or HttpOnly cookies (Web).
abstract final class PreferenceKeys {
  static const locale = 'app_locale';
}
