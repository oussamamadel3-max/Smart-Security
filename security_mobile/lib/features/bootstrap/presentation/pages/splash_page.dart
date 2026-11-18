import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/router/app_routes.dart';
import '../../../../core/session/auth_session_controller.dart';
import '../../../../core/session/auth_session_state.dart';

class SplashPage extends ConsumerStatefulWidget {
  const SplashPage({super.key});

  @override
  ConsumerState<SplashPage> createState() => _SplashPageState();
}

class _SplashPageState extends ConsumerState<SplashPage> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _bootstrap());
  }

  Future<void> _bootstrap() async {
    await ref.read(authSessionProvider.notifier).restoreSession();
    if (!mounted) return;
    final status = ref.read(authSessionProvider).status;
    context.go(
      status == AuthStatus.authenticated ? AppRoutes.home : AppRoutes.login,
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: DesignTokens.background,
      body: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              context.tr.appName,
              style: Theme.of(context).textTheme.headlineLarge,
            ),
            const SizedBox(height: DesignTokens.spaceLg),
            const SizedBox(
              width: 28,
              height: 28,
              child: CircularProgressIndicator(strokeWidth: 2.5),
            ),
          ],
        ),
      ),
    );
  }
}
