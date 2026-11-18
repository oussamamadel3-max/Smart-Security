import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'auth_session_state.dart';

/// Monotonic epoch bumped when a session ends or the authenticated user changes.
///
/// In-flight reads capture [capture] before awaiting and skip cache writes when
/// [isCurrent] is false, so a late success cannot refill the previous user's cache.
class SessionEpoch {
  int _generation = 0;

  int get generation => _generation;

  int capture() => _generation;

  void invalidate() => _generation++;

  bool isCurrent(int captured) => captured == _generation;
}

final sessionEpochProvider = Provider<SessionEpoch>((ref) {
  return SessionEpoch();
});

/// UI may apply a response only for the same authenticated generation.
bool sessionResultApplies({
  required int capturedGeneration,
  required AuthSessionState session,
}) {
  return session.isAuthenticated && session.generation == capturedGeneration;
}
