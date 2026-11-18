import '../../features/auth/domain/entities/auth_user_entity.dart';

enum AuthStatus {
  /// Cold start / restore in progress.
  unknown,

  /// No usable session.
  unauthenticated,

  /// Access + user present; Refresh may live in HttpOnly cookie.
  authenticated,

  /// Refresh failed permanently — guard sends user to login.
  sessionExpired,
}

class AuthSessionState {
  const AuthSessionState({
    required this.status,
    this.user,
    this.generation = 0,
  });

  final AuthStatus status;
  final AuthUserEntity? user;

  /// Bumped on logout / hard clear so late refresh cannot revive the session.
  final int generation;

  static const initial = AuthSessionState(status: AuthStatus.unknown);

  bool get isAuthenticated =>
      status == AuthStatus.authenticated && user != null;

  bool get shouldForceLogin =>
      status == AuthStatus.unauthenticated ||
      status == AuthStatus.sessionExpired;

  AuthSessionState copyWith({
    AuthStatus? status,
    AuthUserEntity? user,
    int? generation,
    bool clearUser = false,
  }) {
    return AuthSessionState(
      status: status ?? this.status,
      user: clearUser ? null : (user ?? this.user),
      generation: generation ?? this.generation,
    );
  }
}
