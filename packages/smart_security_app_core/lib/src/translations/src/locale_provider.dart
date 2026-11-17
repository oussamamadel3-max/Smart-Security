import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../../constants/locale_constants.dart';
import '../../constants/storage_keys.dart';

export '../../constants/locale_constants.dart';

final sharedPreferencesProvider = Provider<SharedPreferences>((ref) {
  throw UnimplementedError(
    'Override sharedPreferencesProvider in ProviderScope:\n\n'
    'overrides: [\n'
    '  sharedPreferencesProvider.overrideWithValue(prefs),\n'
    ']',
  );
});

class LocaleNotifier extends Notifier<Locale> {
  @override
  Locale build() {
    final prefs = ref.read(sharedPreferencesProvider);
    final saved = prefs.getString(PreferenceKeys.locale);

    if (saved != null) {
      final locale = Locale(saved);
      if (kSupportedLocales.contains(locale)) return locale;
    }
    return kDefaultLocale;
  }

  Future<void> setLocale(Locale locale) async {
    if (!kSupportedLocales.contains(locale)) return;
    final prefs = ref.read(sharedPreferencesProvider);
    await prefs.setString(PreferenceKeys.locale, locale.languageCode);
    state = locale;
  }

  Future<void> toggle() async {
    final next =
        state.languageCode == 'ar' ? const Locale('en') : const Locale('ar');
    await setLocale(next);
  }

  bool get isArabic => state.languageCode == 'ar';
  bool get isRTL =>
      state.languageCode == 'ar' ||
      state.languageCode == 'he' ||
      state.languageCode == 'fa';
}

final localeProvider = NotifierProvider<LocaleNotifier, Locale>(
  LocaleNotifier.new,
);
