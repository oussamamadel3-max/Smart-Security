import 'package:flutter_riverpod/flutter_riverpod.dart';

/// Structural session for T-01 only. Cookie/CSRF auth is WEB-T-02.
enum BootstrapAuthStatus { unauthenticated, authenticated }

enum BootstrapUiRole { owner, admin }

class SessionBootstrapState {
  const SessionBootstrapState({
    required this.status,
    this.role = BootstrapUiRole.owner,
  });

  final BootstrapAuthStatus status;
  final BootstrapUiRole role;

  SessionBootstrapState copyWith({
    BootstrapAuthStatus? status,
    BootstrapUiRole? role,
  }) {
    return SessionBootstrapState(
      status: status ?? this.status,
      role: role ?? this.role,
    );
  }
}

class SessionBootstrapNotifier extends Notifier<SessionBootstrapState> {
  @override
  SessionBootstrapState build() => const SessionBootstrapState(
        status: BootstrapAuthStatus.unauthenticated,
      );

  void previewAs(BootstrapUiRole role) {
    state = SessionBootstrapState(
      status: BootstrapAuthStatus.authenticated,
      role: role,
    );
  }

  void clear() => state = const SessionBootstrapState(
        status: BootstrapAuthStatus.unauthenticated,
      );
}

final sessionBootstrapProvider =
    NotifierProvider<SessionBootstrapNotifier, SessionBootstrapState>(
  SessionBootstrapNotifier.new,
);
