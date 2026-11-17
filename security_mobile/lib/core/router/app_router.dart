import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/auth/presentation/pages/login_placeholder_page.dart';
import '../../features/bootstrap/presentation/pages/home_shell_page.dart';
import '../../features/bootstrap/presentation/pages/splash_page.dart';
import '../session/session_bootstrap.dart';
import 'app_routes.dart';

final appRouterProvider = Provider<GoRouter>((ref) {
  final authListenable = ValueNotifier<BootstrapAuthStatus>(
    ref.read(sessionBootstrapProvider),
  );
  ref.listen<BootstrapAuthStatus>(sessionBootstrapProvider, (_, next) {
    authListenable.value = next;
  });
  ref.onDispose(authListenable.dispose);

  return GoRouter(
    initialLocation: AppRoutes.splash,
    refreshListenable: authListenable,
    redirect: (context, state) {
      final status = ref.read(sessionBootstrapProvider);
      final loc = state.matchedLocation;
      final onLogin = loc == AppRoutes.login;
      final onSplash = loc == AppRoutes.splash;

      if (status == BootstrapAuthStatus.unknown || onSplash) {
        return null;
      }
      if (status == BootstrapAuthStatus.unauthenticated) {
        return onLogin ? null : AppRoutes.login;
      }
      if (onLogin) return AppRoutes.home;
      return null;
    },
    routes: [
      GoRoute(
        path: AppRoutes.splash,
        builder: (_, __) => const SplashPage(),
      ),
      GoRoute(
        path: AppRoutes.login,
        builder: (_, __) => const LoginPlaceholderPage(),
      ),
      GoRoute(
        path: AppRoutes.home,
        builder: (_, __) => const HomeShellPage(),
      ),
    ],
  );
});
