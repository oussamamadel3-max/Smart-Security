---
name: flutter-senior-engineer
description: Single source of truth for the Flutter Definition of Done and the eight cross-cutting invariants. Use before finishing any feature, API, CRUD, auth, pagination, cache, refactor, performance, or review task, and whenever another skill says to cross-check flutter-senior-engineer.
paths:
  - "security_mobile/**/*.dart"
  - "admin_web/**/*.dart"
  - "security_mobile/**/pubspec.yaml"
  - "admin_web/**/pubspec.yaml"
  - "security_mobile/**/pubspec.lock"
  - "admin_web/**/pubspec.lock"
---

# Goal

Hold the Definition of Done and the eight cross-cutting invariants in one place.

Other skills apply this file. They must not fork a second checklist.

Read the matching `.cursor/rules/flutter/*.mdc` files for the layers touched. This skill does not replace those rules — it states what "done" means across them.

---

# When To Use

Use when another skill says to cross-check `flutter-senior-engineer`, when finishing generated code, or when reviewing a feature or the project.

---

# Eight cross-cutting invariants

Check these one by one. A layer can look clean and still fail here.

1. **`HttpErrorFailure`** — spelled correctly everywhere. Never `HttpFailure`. No twelfth Failure type; use the eleven in `failure.dart`.
2. **Retry** — HTTP-method based, consistent with `network-architecture.mdc` / `repository.mdc` / `error-handling.mdc`. GET via `executeWithRetry()`. POST/DELETE never auto-retried without an explicit idempotency key.
3. **CancelToken** — every screen-scoped token for upload, download, pagination, or search is created and released on controller dispose (`ref.onDispose()` on Riverpod, `close()` on BLoC).
4. **Unauthorized navigation** — exactly one path: `AuthInterceptor` updates the session owner (`onSessionExpired` → `authSessionProvider` or `AuthCubit`/`AuthBloc`). `authGuardRedirect` is the only code that redirects. No `context.go(AppRoutes.login)` from a presentation controller, repository, or catch block on auth failure. Explicit logout in the UI is allowed.
5. **Certificate pinning** — configured in `lib/core/network/` for every government/military endpoint this work touches. "Will pin later" is a fail.
6. **One controller per screen** — Riverpod: ViewModel **is** the Notifier (`LoginViewModel extends AsyncNotifier<LoginState>`). BLoC: Cubit (default) or Bloc **is** the controller. Never ViewModel+Notifier or ViewModel+Cubit. Never mix Riverpod and BLoC in one app (`state-management.mdc`).
7. **Theme** — `AppTheme`, `AppColors`, `AppTypography`, `AppSemanticColors`, and the `My*` design-system widgets. No `Colors.blue`, ad-hoc hex, or Material defaults for brand UI. Spacing matches nearby screens / theme radii; do not invent a second spacing system.
8. **Locale** — `context.tr` for strings. Locale switching via the stack's locale owner (`localeProvider` or `LocaleCubit`). Never `AppLocalizations.of(context)` in feature code. Every new string exists in both `app_en.arb` and `app_ar.arb`.

A feature that passes every layer rule but fails one of these eight is not done.

---

# Definition of Done

Skip sections whose layers were not touched. Do not skip a section by assumption.

## Layering

- Feature stays under `lib/features/<name>/` with Domain / Data / Presentation split (`architecture.mdc`).
- Domain does not import Flutter, Dio, JSON, or Isar.
- Presentation does not import Data types (DTO, Api, DataSource).
- DTO never leaves Data. Entity is used outside Data.
- The presentation controller does not call Api or DataSource. If UseCases exist, it does not call the repository.

## State and data

- Repository is the single source of truth (`repository.mdc`).
- One presentation controller per screen, matching `state-management.mdc` (Riverpod **or** BLoC, never both).
- State is immutable and holds Entity / `ErrorState` only.
- UI shows one primary visual state (Loading → Error → Empty → Content → Refreshing).

## Errors

- `Result<T>` at Data/Domain boundary.
- New failures go through `ErrorMapper` → `Failure` → `FailureHandler` → `ErrorState`.
- UI uses `MySnackbar` / `MyDialog` / `MyInlineError` / `MyEmptyState` / `ErrorStateHost`.

## Security

- Secrets in `SessionStorage` / `flutter_secure_storage` only (`security.mdc`).
- Pinning, screenshot protection, and session auto-lock applied when the data classification requires them.

## Presentation

- Pages compose; widgets stay small; no business logic in `build`.
- Forms follow the existing reactive_forms rule (`general-best-practices.mdc`).
- Routes registered in `AppRouter` / `AppRoutes` when the feature is reachable.

## Naming

- Suffixes and `my_` rule from `naming-conventions.mdc`.
- Invariant 1 and 6 above.

## Tests

Required in this order for new/changed behavior: UseCase → Repository → Mapper → presentation controller (ViewModel or Cubit with `bloc_test`). Widget tests only when the widget has logic beyond rendering.

A feature without UseCase/Repository tests is not done, even if it compiles.

Mock external dependencies only — never mock the business rule under test.

---

# How to apply

1. Identify layers touched.
2. Read the corresponding rule files.
3. Walk this Definition of Done.
4. Walk the eight invariants explicitly.
5. Fix violations before returning.

Do not restate this checklist inside other skills. Point here.
