---
name: flutter-implement-auth
description: Implement or extend authentication while preserving the project's architecture.
paths:
  - "security_mobile/**/*.dart"
  - "admin_web/**/*.dart"
  - "security_mobile/**/pubspec.yaml"
  - "admin_web/**/pubspec.yaml"
  - "security_mobile/**/pubspec.lock"
  - "admin_web/**/pubspec.lock"
---

# Goal
Implement authentication using the project's existing infrastructure.
Support login, logout and session management.
Never introduce a second authentication flow.
---
# When To Use
Use this skill when
- adding authentication
- extending authentication
- integrating login APIs
- implementing token refresh
---
# Execution Principles
- Inspect before generating.
- Reuse before creating.
- Preserve existing authentication flow.
- Never duplicate authentication state.
- Follow project Rules.
- Cross-check the cross-cutting invariants and Definition of Done in the `flutter-senior-engineer` skill before finishing — the unauthorized-navigation invariant below is one of them; don't re-derive it, just apply it.
---
# Required Inputs
Collect only missing information.
Examples
- Login endpoint
- Refresh endpoint
- Authentication type
- User model
---
# Workflow
## Step 1 — Inspect
Locate
- Auth Repository
- Auth presentation controller (ViewModel or Cubit)
- Secure Storage
- Interceptors
- Router Guards
---
## Step 2 — Analyze
Understand the authentication flow.
Determine
- Login
- Logout
- Refresh
- Session validation
---
## Step 3 — Integrate
Update only the necessary files.
Possible updates
- DTO
- Api
- Repository
- Presentation controller (ViewModel or Cubit)
- Interceptor
- Router

The unauthorized path has exactly one route, and this skill must not deviate from it:

`AuthInterceptor` attempts a token refresh first. If the refresh fails, the interceptor calls `onSessionExpired` so the **session owner** becomes `sessionExpired` (Riverpod: `authSessionProvider`; BLoC: `AuthCubit` / existing `AuthBloc`) — it does not navigate itself, does not call `context.go()`, and does not throw a raw exception to the caller to handle ad hoc. `authGuardRedirect` is the only thing that reacts to the session owner and redirects (login, splash, or `SessionExpiredPage`) via the router's `redirect` callback.

`AuthInterceptor` responsibilities are limited to: performing the refresh, updating session state on failure, and preventing infinite retry loops if refresh itself keeps failing (cap retries, don't let a dead refresh token spin). Business logic beyond that does not belong in the interceptor.

Never add a second place that calls `context.go(AppRoutes.login)` on an auth failure — not in a presentation controller, not in a Repository, not in a one-off catch block for "this one screen." Explicit user logout in the UI may navigate after it resets session state. If a screen seems to need its own auth-failure redirect, the guard isn't covering a route it should — fix the guard, not the screen.

Token storage: access token, refresh token, and session data go through `flutter_secure_storage` exclusively — never `SharedPreferences`, not even "temporarily."
---
## Step 4 — Session Security Integration
Check whether this project's `security.mdc` requirements apply here and wire them into the same auth state, rather than treating them as unrelated:
- If session auto-lock is required, locking must route through the same re-authentication flow as a normal session expiry — not a separate lock screen with its own logic.
- If device integrity checks (root/jailbreak detection) are required, a compromised device is treated as equivalent to an unauthenticated state for any protected route, using the same guard mechanism — not a second, parallel block.
---
## Step 5 — Test
Add tests covering:
- successful login populates the session owner correctly and secure storage receives the tokens
- refresh succeeds transparently on a token-expired request (the original request completes without the caller knowing a refresh happened)
- refresh fails → session owner becomes `sessionExpired` → guard redirect is triggered (test the state transition; router redirect itself may need a widget/integration test)
- logout clears every stored credential, not just the access token, and resets the session owner
- repeated refresh failures do not loop indefinitely

Authentication without a test for the refresh-fails path is not complete — this is the path most likely to be broken silently.
---
## Step 6 — Review
Validate the complete authentication flow.
Ensure unauthorized users cannot access protected routes.
Confirm no code path outside `authGuardRedirect` performs an auth-driven navigation.
Confirm against `flutter-senior-engineer`'s Definition of Done for the layers touched.
---
# Expected Output
Authentication integrated.
Single source of truth.
Secure token management: access token, refresh token, and session data all in `flutter_secure_storage`, nowhere else.
---
# Self Review
Before finishing verify
- Secure Storage used for every credential (access token, refresh token, session)
- Refresh implemented, with a cap on retry loops
- Logout clears session fully, not just the access token
- Protected routes enforced
- The only navigation on auth failure happens through `authGuardRedirect` — no `context.go(AppRoutes.login)` from interceptors or controller catch blocks
- Session auto-lock and device-integrity checks (where required by `security.mdc`) route through the same session owner, not a separate flow
- No duplicated auth logic
- Tests cover login, transparent refresh, refresh failure → guard redirect, logout, and repeated-failure behavior
