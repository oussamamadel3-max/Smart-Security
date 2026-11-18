import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/router/app_routes.dart';
import '../../../../core/router/auth_guard.dart';
import '../../../../core/session/auth_session_controller.dart';

/// Shown when GoRouter has no match. Copy stays in the web app.
class NotFoundPage extends ConsumerWidget {
  const NotFoundPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final session = ref.watch(authSessionProvider);
    final theme = Theme.of(context);
    return Scaffold(
      backgroundColor: DesignTokens.background,
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(DesignTokens.spaceXl),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 420),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(
                    '404',
                    style: theme.textTheme.headlineLarge?.copyWith(
                      color: DesignTokens.primary,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  const SizedBox(height: DesignTokens.spaceSm),
                  Text(
                    'الصفحة غير موجودة',
                    textAlign: TextAlign.center,
                    style: theme.textTheme.titleLarge,
                  ),
                  const SizedBox(height: DesignTokens.spaceSm),
                  Text(
                    'هذا المسار غير مسجّل في تطبيق الويب.',
                    textAlign: TextAlign.center,
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: DesignTokens.muted,
                    ),
                  ),
                  const SizedBox(height: DesignTokens.spaceXl),
                  FilledButton(
                    onPressed: () {
                      final user = session.user;
                      if (session.isAuthenticated && user != null) {
                        context.go(homeRouteFor(user));
                        return;
                      }
                      context.go(AppRoutes.login);
                    },
                    child: Text(context.tr.home),
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
