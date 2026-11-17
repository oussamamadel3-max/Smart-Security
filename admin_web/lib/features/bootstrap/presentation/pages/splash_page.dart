import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/router/app_routes.dart';
import '../../../../core/session/session_bootstrap.dart';

class SplashPage extends ConsumerStatefulWidget {
  const SplashPage({super.key});

  @override
  ConsumerState<SplashPage> createState() => _SplashPageState();
}

class _SplashPageState extends ConsumerState<SplashPage> {
  @override
  void initState() {
    super.initState();
    Future<void>.delayed(const Duration(milliseconds: 300), () {
      if (!mounted) return;
      final session = ref.read(sessionBootstrapProvider);
      if (session.status == BootstrapAuthStatus.authenticated) {
        context.go(
          session.role == BootstrapUiRole.admin
              ? AppRoutes.adminHome
              : AppRoutes.ownerHome,
        );
      } else {
        context.go(AppRoutes.login);
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Center(
        child: Text(
          context.tr.appName,
          style: Theme.of(context).textTheme.headlineLarge,
        ),
      ),
    );
  }
}
