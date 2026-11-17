import 'package:flutter/material.dart';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../translations/src/locale_provider.dart';
import '../translations/src/localization_extension.dart';

class LanguageDialog {
  static void show(BuildContext context, WidgetRef ref, Locale currentLocale) {
    final tr = context.tr;
    showDialog(
      context: context,
      builder: (dialogContext) {
        return AlertDialog(
          title: Text(tr.language),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              _tile(
                ref: ref,
                context: context,
                dialogContext: dialogContext,
                locale: const Locale('ar'),
                current: currentLocale,
                title: tr.arabic,
                icon: Icons.language,
              ),
              _tile(
                ref: ref,
                context: context,
                dialogContext: dialogContext,
                locale: const Locale('en'),
                current: currentLocale,
                title: tr.english,
                icon: Icons.language,
              ),
            ],
          ),
        );
      },
    );
  }

  static Widget _tile({
    required WidgetRef ref,
    required BuildContext context,
    required BuildContext dialogContext,
    required Locale locale,
    required Locale current,
    required String title,
    required IconData icon,
  }) {
    final selected = locale.languageCode == current.languageCode;
    final textTheme = Theme.of(context).textTheme;
    final colorScheme = Theme.of(context).colorScheme;
    return ListTile(
      leading: Icon(
        selected ? Icons.check_circle : icon,
        color: selected ? colorScheme.primary : colorScheme.onSurfaceVariant,
      ),
      title: Text(
        title,
        style: textTheme.labelSmall?.copyWith(
          color: selected ? colorScheme.primary : colorScheme.onSurfaceVariant,
          fontWeight: selected ? FontWeight.w700 : FontWeight.w500,
        ),
      ),
      onTap: () {
        ref.read(localeProvider.notifier).setLocale(locale);
        Navigator.pop(dialogContext);
      },
    );
  }
}
