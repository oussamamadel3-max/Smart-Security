import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/config/app_environment.dart';
import '../../../../core/session/session_bootstrap.dart';

class HomeShellPage extends ConsumerWidget {
  const HomeShellPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final tr = context.tr;
    final env = AppEnvironment.current;

    return Scaffold(
      appBar: AppBar(
        title: Text(tr.appName),
        actions: [
          IconButton(
            tooltip: tr.language,
            onPressed: () => LanguageDialog.show(
              context,
              ref,
              ref.read(localeProvider),
            ),
            icon: const Icon(Icons.language),
          ),
          IconButton(
            tooltip: tr.logout,
            onPressed: () =>
                ref.read(sessionBootstrapProvider.notifier).clear(),
            icon: const Icon(Icons.logout),
          ),
        ],
      ),
      body: MyEmptyState(
        type: MyEmptyType.empty,
        title: tr.bootstrapPlaceholder,
        subtitle: 'Mobile · ${env.label} · ${env.apiBaseUrl}',
        primaryActionLabel: tr.language,
        onPrimaryAction: () => LanguageDialog.show(
          context,
          ref,
          ref.read(localeProvider),
        ),
      ),
    );
  }
}
