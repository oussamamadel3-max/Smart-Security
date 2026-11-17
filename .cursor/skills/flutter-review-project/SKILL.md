---
name: flutter-review-project
description: Review the entire Flutter codebase for architecture drift, duplicated infrastructure, and rule violations across features. Use when auditing the whole app, preparing a release-wide quality pass, or when the Review Project command runs.
paths:
  - "security_mobile/**/*.dart"
  - "admin_web/**/*.dart"
  - "security_mobile/**/pubspec.yaml"
  - "admin_web/**/pubspec.yaml"
  - "security_mobile/**/pubspec.lock"
  - "admin_web/**/pubspec.lock"
---

# Goal

Assess the **whole** Flutter application: every feature plus `lib/core/`. Find inconsistencies between features, duplicated infrastructure, and rule drift.

Do not rewrite code unless explicitly requested.

Criteria come from `flutter-senior-engineer` and the project Rules — do not invent a second standard.

---

# When To Use

Use when reviewing the complete project, comparing features to each other, or producing a backlog of architecture work.

Do not use for a single-feature review — use Review Flutter Feature.

---

# Workflow

## Step 1 — Inventory

List every feature under `lib/features/` and every shared module under `lib/core/`. Note which layers each feature actually has.

## Step 2 — Core

Review shared infrastructure once: network, errors, session, router, theme, widgets, storage. Drift here is Critical because every feature inherits it.

Confirm `authGuardRedirect` is still the only auth-driven navigation, `ErrorMapper` is the only HTTP-to-Failure bridge, and `SessionStorage` is the only token store.

## Step 3 — Features

For each feature, apply `flutter-senior-engineer` Definition of Done at a **summary** level (layering, errors, naming, tests). Dive deep only where the inventory shows a missing layer, a second Dio client, a DTO leak, or a custom error path.

## Step 4 — Cross-feature

Look for what a single-feature review cannot see:

- two networking stacks
- two auth redirect paths
- inconsistent controller splits (ViewModel+Notifier, or ViewModel+Cubit)
- mixed Riverpod and BLoC in one app
- `my_` prefix on feature widgets
- strings in only one ARB file
- copy-pasted mappers/retry loops

## Step 5 — Release readiness (only if the user asked for a release review)

Obfuscation, pinning on government/military endpoints, debug logging off, device-integrity and session auto-lock if `security.mdc` requires them for shipped data.

Skip this step for a routine project audit.

## Step 6 — Prioritize and report

```markdown
# Project review

## Inventory
- features: …
- core modules: …

## Strengths
- …

## Project-wide findings
| Severity | Area | Finding | Rule / invariant | Fix |
|---|---|---|---|---|
| Critical | core/network | … | … | … |

## Per-feature notes
- auth: …
- home: …

## Backlog (ordered)
1. …
```

Cross-cutting or core findings are never Low.

---

# Expected Output

One project assessment with a prioritized backlog. No drive-by refactors.

---

# Self Review

- Every feature listed
- Core reviewed once
- Cross-feature inconsistencies called out
- Findings tied to rules / invariants
- Release section included only when asked
