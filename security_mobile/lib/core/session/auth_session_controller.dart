import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../features/auth/domain/entities/auth_session_tokens.dart';
import '../../features/auth/domain/entities/auth_user_entity.dart';
import '../../features/auth/domain/repositories/auth_repository.dart';
import '../../features/auth/domain/usecases/login_usecase.dart';
import '../../features/auth/domain/usecases/logout_usecase.dart';
import '../../features/auth/domain/usecases/restore_session_usecase.dart';
import '../../features/notifications/presentation/providers/push_tokens_providers.dart';
import '../network/auth_dependency_providers.dart';
import 'access_token_holder.dart';
import 'auth_session_state.dart';
import 'refresh_token_store.dart';

/// Single session owner for the mobile app (`authSessionProvider`).
class AuthSessionController extends Notifier<AuthSessionState> {
  bool _logoutInProgress = false;

  AccessTokenHolder get _access => ref.read(accessTokenHolderProvider);
  RefreshTokenStore get _refreshStore => ref.read(refreshTokenStoreProvider);
  AuthRepository get _repository => ref.read(authRepositoryProvider);

  bool get isLogoutInProgress => _logoutInProgress;

  @override
  AuthSessionState build() => AuthSessionState.initial;

  Future<void> restoreSession() async {
    state = state.copyWith(status: AuthStatus.unknown, clearUser: true);
    final refresh = await _refreshStore.read();
    if (refresh == null) {
      _access.clear();
      state = state.copyWith(
        status: AuthStatus.unauthenticated,
        clearUser: true,
      );
      return;
    }

    final generation = state.generation;
    final result =
        await RestoreSessionUseCase(_repository)(refreshToken: refresh);
    if (generation != state.generation || _logoutInProgress) {
      return;
    }

    await result.fold(
      onFailure: (_) async {
        await _hardClear(sessionExpired: false);
      },
      onSuccess: (tokens) async {
        if (!_acceptOwner(tokens.user)) {
          await _hardClear(sessionExpired: false);
          return;
        }
        await _persistSession(tokens, generation: generation);
      },
    );
  }

  Future<Result<AuthUserEntity>> login({
    required String userName,
    required String password,
  }) async {
    final result = await LoginUseCase(_repository)(
      userName: userName,
      password: password,
    );

    switch (result) {
      case Failure_(:final failure):
        return Failure_(failure);
      case Success(:final data):
        if (!_acceptOwner(data.user)) {
          await _refreshStore.clear();
          _access.clear();
          return const Failure_(
            Failure.forbidden(
              message: 'هذا التطبيق مخصص لحساب المالك (OWNER).',
            ),
          );
        }
        await _persistSession(data, generation: state.generation);
        return Success(data.user);
    }
  }

  /// Single-flight refresh target used by AuthInterceptor.
  Future<bool> refreshSession() async {
    if (_logoutInProgress) return false;

    final generation = state.generation;
    final refresh = await _refreshStore.read();
    if (refresh == null) return false;

    final result =
        await RestoreSessionUseCase(_repository)(refreshToken: refresh);
    if (_logoutInProgress || generation != state.generation) {
      // Late refresh after logout must not restore credentials.
      return false;
    }

    switch (result) {
      case Failure_():
        if (!_logoutInProgress && generation == state.generation) {
          await _hardClear(sessionExpired: true);
        }
        return false;
      case Success(:final data):
        if (_logoutInProgress || generation != state.generation) {
          return false;
        }
        if (!_acceptOwner(data.user)) {
          await _hardClear(sessionExpired: true);
          return false;
        }
        await _persistSession(data, generation: generation);
        return true;
    }
  }

  void markSessionExpired() {
    if (_logoutInProgress) return;
    _access.clear();
    // Keep refresh cleared on hard expiry so cold start cannot revive.
    // ignore: discarded_futures
    _refreshStore.clear();
    state = state.copyWith(
      status: AuthStatus.sessionExpired,
      clearUser: true,
      generation: state.generation + 1,
    );
  }

  Future<void> logout() async {
    if (_logoutInProgress) return;
    _logoutInProgress = true;
    final generation = state.generation;
    final refresh = await _refreshStore.read();

    try {
      // Revoke push binding while Access JWT is still valid.
      try {
        await ref.read(pushTokensRepositoryProvider).revokeStoredOrAll();
      } catch (_) {
        // Logout must continue even if revoke fails.
      }
      await LogoutUseCase(_repository)(refreshToken: refresh);
    } catch (_) {
      // Local clear still required.
    } finally {
      await _hardClear(
        sessionExpired: false,
        bumpGenerationFrom: generation,
      );
      _logoutInProgress = false;
    }
  }

  Future<void> _persistSession(
    AuthSessionTokens tokens, {
    required int generation,
  }) async {
    if (_logoutInProgress || generation != state.generation) {
      return;
    }
    _access.setToken(tokens.accessToken);
    await _refreshStore.write(tokens.refreshToken);
    if (_logoutInProgress || generation != state.generation) {
      _access.clear();
      await _refreshStore.clear();
      return;
    }
    state = state.copyWith(
      status: AuthStatus.authenticated,
      user: tokens.user,
    );
  }

  Future<void> _hardClear({
    required bool sessionExpired,
    int? bumpGenerationFrom,
  }) async {
    _access.clear();
    await _refreshStore.clear();
    final nextGen = (bumpGenerationFrom ?? state.generation) + 1;
    state = AuthSessionState(
      status: sessionExpired
          ? AuthStatus.sessionExpired
          : AuthStatus.unauthenticated,
      generation: nextGen,
    );
  }

  bool _acceptOwner(AuthUserEntity user) {
    if (user.isOwner) return true;
    if (user.roles.isEmpty) return false;
    return user.roles.any((r) => r.toUpperCase() == AppRoleCodes.owner);
  }
}

final authSessionProvider =
    NotifierProvider<AuthSessionController, AuthSessionState>(
  AuthSessionController.new,
);
