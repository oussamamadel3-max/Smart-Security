import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/config/app_environment.dart';
import '../../../../core/session/session_bootstrap.dart';

/// Structural login shell only — real Auth API is MOB-T-02.
class LoginPlaceholderPage extends ConsumerWidget {
  const LoginPlaceholderPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final tr = context.tr;
    final env = AppEnvironment.current;

    return Scaffold(
      body: SafeArea(
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 420),
            child: Padding(
              padding: const EdgeInsets.all(DesignTokens.spaceXl),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text(tr.appName, style: Theme.of(context).textTheme.headlineLarge),
                  const SizedBox(height: DesignTokens.spaceSm),
                  Text(
                    tr.appTitle,
                    style: Theme.of(context).textTheme.bodyMedium,
                  ),
                  const SizedBox(height: DesignTokens.spaceMd),
                  Text(
                    'بيئة التشغيل: ${env.label}',
                    style: Theme.of(context).textTheme.labelMedium,
                  ),
                  const SizedBox(height: DesignTokens.spaceXl),
                  Text(
                    'تسجيل الدخول الحقيقي في المهمة MOB-T-02 (Access في الذاكرة + Refresh في التخزين الآمن).',
                    style: Theme.of(context).textTheme.bodySmall,
                  ),
                  const SizedBox(height: DesignTokens.spaceLg),
                  ElevatedButton(
                    onPressed: () {
                      ref
                          .read(sessionBootstrapProvider.notifier)
                          .markAuthenticatedForUiPreview();
                    },
                    child: Text(tr.continueText),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
