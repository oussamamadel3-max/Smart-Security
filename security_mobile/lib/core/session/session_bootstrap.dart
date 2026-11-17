import 'package:flutter_riverpod/flutter_riverpod.dart';

/// Structural session only for T-01 route guards.
/// Full JWT / refresh / secure storage arrives in MOB-T-02.
enum BootstrapAuthStatus { unknown, unauthenticated, authenticated }

class SessionBootstrapNotifier extends Notifier<BootstrapAuthStatus> {
  @override
  BootstrapAuthStatus build() => BootstrapAuthStatus.unauthenticated;

  void markAuthenticatedForUiPreview() {
    // Preview helper for layout only — not a real login.
    state = BootstrapAuthStatus.authenticated;
  }

  void clear() => state = BootstrapAuthStatus.unauthenticated;
}

final sessionBootstrapProvider =
    NotifierProvider<SessionBootstrapNotifier, BootstrapAuthStatus>(
  SessionBootstrapNotifier.new,
);
