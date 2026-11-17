import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

// ValueNotifier is used as GoRouter.refreshListenable.

import '../../features/admin/presentation/pages/admin_home_placeholder_page.dart';
import '../../features/auth/presentation/pages/login_placeholder_page.dart';
import '../../features/bootstrap/presentation/pages/splash_page.dart';
import '../../features/owner/presentation/pages/owner_home_placeholder_page.dart';
import '../session/session_bootstrap.dart';
import 'app_routes.dart';

final appRouterProvider = Provider<GoRouter>((ref) {
  final listenable = ValueNotifier<SessionBootstrapState>(
    ref.read(sessionBootstrapProvider),
  );
  ref.listen<SessionBootstrapState>(sessionBootstrapProvider, (_, next) {
    listenable.value = next;
  });
  ref.onDispose(listenable.dispose);

  return GoRouter(
    initialLocation: AppRoutes.splash,
    refreshListenable: listenable,
    redirect: (context, state) {
      final session = ref.read(sessionBootstrapProvider);
      final loc = state.matchedLocation;
      final onLogin = loc == AppRoutes.login;
      final onSplash = loc == AppRoutes.splash;

      if (onSplash) return null;

      if (session.status == BootstrapAuthStatus.unauthenticated) {
        return onLogin ? null : AppRoutes.login;
      }

      if (onLogin) {
        return session.role == BootstrapUiRole.admin
            ? AppRoutes.adminHome
            : AppRoutes.ownerHome;
      }

      if (loc.startsWith(AppRoutes.adminHome) &&
          session.role != BootstrapUiRole.admin) {
        return AppRoutes.ownerHome;
      }
      if (loc.startsWith(AppRoutes.ownerHome) &&
          session.role != BootstrapUiRole.owner) {
        return AppRoutes.adminHome;
      }
      return null;
    },
    routes: [
      GoRoute(path: AppRoutes.splash, builder: (_, __) => const SplashPage()),
      GoRoute(
        path: AppRoutes.login,
        builder: (_, __) => const LoginPlaceholderPage(),
      ),
      GoRoute(
        path: AppRoutes.ownerHome,
        builder: (_, __) => const OwnerHomePlaceholderPage(),
      ),
      GoRoute(
        path: AppRoutes.adminHome,
        builder: (_, __) => const AdminHomePlaceholderPage(),
      ),
    ],
  );
});
