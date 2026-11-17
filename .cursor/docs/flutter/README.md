# Cursor Project Engineering Framework

> **Location (Smart Security monorepo):** Cursor discovers this framework from `.cursor/commands`, `.cursor/rules/flutter`, `.cursor/skills`, `.cursor/templates/flutter`, and `.cursor/docs/flutter`. Flutter rules apply only under `security_mobile/` and `admin_web/`.


Engineering framework for AI-assisted Flutter development on **training_indicator_gis_app**.

Open `commands/training_indicator_gis_app.code-workspace` so the Flutter app and this framework load together. The on-disk app folder is still `training_indiator_gis_app` (legacy spelling); do not change that path unless the Flutter repo is renamed.

The framework has four layers. Use them in this order. Do not skip a layer or invent a parallel architecture.

```text
Commands
    ↓
Skills
    ↓
Templates
    ↓
Rules
    ↓
Generated Code
```

| Layer | Role |
|---|---|
| **Commands** | Short user-facing entry. Collect missing input, then dispatch to a Skill. |
| **Skills** | Step-by-step workflow, tests, and Self Review. |
| **Templates** | Per-layer contracts (what a file may own and import). |
| **Rules** | Always-on / glob-scoped invariants the agent must follow. |

`flutter-senior-engineer` is the single Definition of Done. Other skills point to it instead of copying checklists.

---

Daily playbook (Arabic, copy-paste prompts): [USAGE.md](USAGE.md).

## How to use

1. Open the workspace file above.
2. Run a Command from [USAGE.md](USAGE.md) (or name the Skill in chat).
3. The agent inspects the Flutter app first, asks only for missing information, then generates or reviews code.
4. Before finishing, it must pass `flutter-senior-engineer` and the matching Rules.

Reuse `lib/core/` (ApiClient, ErrorMapper, GoRouter, SessionStorage, `My*` widgets). Do not duplicate infrastructure inside a feature.

---

## Commands → Skills

| Command | Skill |
|---|---|
| `flutter-0-choose_stack` | يكتب `.cursor/docs/flutter/stack.md` (لا مهارة منفصلة) |
| `flutter-1-create-feature` | `.cursor/skills/flutter-create-feature` |
| `flutter-2-integrate-api` | `.cursor/skills/flutter-integrate-api` |
| `flutter-3-review-feature` | `.cursor/skills/flutter-review-feature` |
| `flutter-4-create-crud` | `.cursor/skills/flutter-create-crud` |
| `flutter-5-implement-auth` | `.cursor/skills/flutter-implement-auth` |
| `flutter-6-implement-pagination` | `.cursor/skills/flutter-implement-pagination` |
| `flutter-7-add-offline-cache` | `.cursor/skills/flutter-add-offline-cache` |
| `flutter-8-refactor-feature` | `.cursor/skills/flutter-refactor-feature` |
| `flutter-9-optimize-performance` | `.cursor/skills/flutter-optimize-performance` |
| `flutter-10-senior-review` | `.cursor/skills/flutter-senior-review` |
| `flutter-11-review-project` | `.cursor/skills/flutter-review-project` |

Supporting skills (not commands): `flutter-senior-engineer` (Definition of Done), `flutter-write-adr` (pattern-level decisions → `docs/adr/` in the Flutter app).

---

## Rules

| File | When it applies |
|---|---|
| `.cursor/rules/flutter/architecture.mdc` | When Flutter files open — layers and folders |
| `naming-conventions.mdc` | When Flutter files open — types, `my_`, ARB, session provider |
| `general-best-practices.mdc` | When Flutter files open — size limits, reuse, forms |
| `security.mdc` | When Flutter files open — tokens, pinning, logging |
| `error-handling.mdc` | Errors, network, features |
| `network-architecture.mdc` | Dio, retry, CancelToken, pinning |
| `repository.mdc` | Repositories and UseCases |
| `state-management.mdc` | When Flutter files open — Riverpod (see stack.md) |
| `state-management-riverpod.mdc` | Riverpod ViewModels |
| `state-management-bloc.mdc` | BLoC Cubits / Blocs |
| `performance.mdc` | Measure-then-fix |

---

## Templates

Feature contracts live in `.cursor/templates/flutter/`. Generate only files the feature needs.

Domain: Entity, UseCase, Repository contract.

Data: DTO, Mapper, Api, RemoteDataSource, LocalDataSource (offline only), Repository impl.

Presentation: State, UI state, controller (ViewModel **or** Cubit), Page, Form, Widget.

Tests: `.cursor/templates/flutter/14-tests.md` — UseCase → Repository → Mapper → controller.

Lock the stack in [stack.md](stack.md) (`auto` | `riverpod` | `bloc`). Never mix.

---

## Stack this framework assumes

**One of:** Riverpod **or** BLoC (see `stack.md`). Shared: GoRouter, Dio (`ApiClient`), Isar (offline), `flutter_secure_storage`, reactive_forms, Freezed, ARB (`context.tr`).

Failure types are the eleven already defined. Spell `HttpErrorFailure`. Session owner is `authSessionProvider` (Riverpod) or `AuthCubit`/`AuthBloc` (BLoC). Auth-driven navigation is `authGuardRedirect` only.
