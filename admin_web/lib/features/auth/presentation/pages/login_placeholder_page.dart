import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/config/app_environment.dart';
import '../../../../core/session/session_bootstrap.dart';

/// Structural login — Cookie/CSRF Auth is WEB-T-02. No Web Storage for tokens.
class LoginPlaceholderPage extends ConsumerWidget {
  const LoginPlaceholderPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final tr = context.tr;
    final env = AppEnvironment.current;

    return Scaffold(
      body: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 480),
          child: Padding(
            padding: const EdgeInsets.all(DesignTokens.spaceXl),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(tr.appName, style: Theme.of(context).textTheme.headlineLarge),
                Text(tr.appTitle, style: Theme.of(context).textTheme.bodyMedium),
                const SizedBox(height: DesignTokens.spaceMd),
                Text(
                  'Web · ${env.label} · لا تُخزَّن رموز Auth في localStorage',
                  style: Theme.of(context).textTheme.labelMedium,
                ),
                const SizedBox(height: DesignTokens.spaceXl),
                ElevatedButton(
                  onPressed: () => ref
                      .read(sessionBootstrapProvider.notifier)
                      .previewAs(BootstrapUiRole.owner),
                  child: const Text('معاينة مسار المالك'),
                ),
                const SizedBox(height: DesignTokens.spaceSm),
                OutlinedButton(
                  onPressed: () => ref
                      .read(sessionBootstrapProvider.notifier)
                      .previewAs(BootstrapUiRole.admin),
                  child: const Text('معاينة مسار الأدمن'),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
