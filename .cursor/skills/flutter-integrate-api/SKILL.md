---
name: flutter-integrate-api
description: Integrate one or more REST endpoints into an existing Flutter feature while preserving the project's architecture.
paths:
  - "security_mobile/**/*.dart"
  - "admin_web/**/*.dart"
  - "security_mobile/**/pubspec.yaml"
  - "admin_web/**/pubspec.yaml"
  - "security_mobile/**/pubspec.lock"
  - "admin_web/**/pubspec.lock"
---

# Goal

Integrate a REST API into an existing feature.

Extend the existing implementation instead of replacing it.

Follow all applicable project Rules.

---

# When To Use

Use this skill when

- adding a new endpoint
- updating an existing endpoint
- connecting a feature to a backend
- replacing mock data with a real API

Do not use this skill to create a new feature.

---

# Execution Principles

- Inspect before generating.
- Reuse before creating.
- Extend before replacing.
- Minimize changes.
- Preserve project architecture.
- Follow project Rules.
- Cross-check the cross-cutting invariants and Definition of Done in the `flutter-senior-engineer` skill before finishing — don't re-derive or re-list them here; that skill is the single source of truth for the invariants that span multiple rule files (retry wording, naming, pinning, etc).

---

# Required Inputs

Inspect the project first.

Collect only missing information.

Possible inputs

- Endpoint
- HTTP Method
- Request Model
- Response Model
- Authentication
- Upload / Download
- Pagination
- Whether the endpoint is government/military (affects pinning requirements)
- Whether the endpoint's data can change after the initial read (affects controller type if one already exists)

Never ask for information already available in the codebase.

---

# Workflow

## Step 1 — Inspect

Understand the existing feature.

Locate

- Repository
- DataSource
- Api
- DTOs
- Entities
- ViewModel

Reuse them whenever possible.

---

## Step 2 — Analyze

Understand the endpoint.

Determine

- Request
- Response
- Errors
- Authentication
- HTTP method (this decides the retry policy in Step 3 — GET is retryable via `executeWithRetry()`; PUT/DELETE only if confirmed idempotent server-side; POST never auto-retried without an explicit idempotency key)

Do not invent missing API contracts.

---

## Step 3 — Integrate

Update only the required files.

Possible updates

- DTO
- Mapper
- Api
- RemoteDataSource
- Repository
- UseCase
- ViewModel

Avoid creating duplicate implementations.

New DTO/Mapper/Api classes follow the project's naming suffixes (`Dto`, `Mapper`, `Api`) — no ad hoc naming even for a "quick" integration.

If the endpoint is Upload, Download, Pagination, or search-as-you-type: wire a `CancelToken` for the request and release it on controller dispose (`ref.onDispose()` or Cubit/Bloc `close()`). This is not optional for these categories — they're exactly the request types this project's cancellation rule targets.

If the endpoint is under a government/military domain: verify certificate pinning is already configured for it in `core/network/` before continuing. If it isn't, that's a blocker for this integration, not a note for later.

---

## Step 4 — Connect

Integrate with

- Result<T>
- Error Pipeline
- Riverpod or BLoC (the project's resolved stack)
- Localization

Only update what is affected.

Any new exception surface this endpoint can produce must be mapped through the existing `ErrorMapper` — never handled with a one-off try/catch local to this integration.

---

## Step 5 — Test

Add or update tests for the Api method and the Repository method that calls it, including at least one case that exercises the error-mapping path (a failure response should surface as the correct `Failure` subtype, not a generic one).

An endpoint integration without a corresponding Repository-level test is not complete, even if it works when manually tried.

---

## Step 6 — Review

Validate the integration.

Ensure existing functionality remains unchanged.

Confirm against `flutter-senior-engineer`'s Definition of Done for the layers touched (Data/Repository/state-management sections apply here; skip the presentation-only items if no UI changed).

---

# Expected Output

The API is fully integrated.

No duplicated code.

No architectural violations.

Only the minimum required files are modified.

---

# Self Review

Before finishing verify

- Existing code reused
- DTO mapped correctly
- Repository updated
- ViewModel / Cubit updated
- Error handling preserved, with the new exception surface mapped to the correct `Failure` type
- Retry behavior matches the HTTP method (GET retryable, POST never auto-retried without an idempotency key)
- CancelToken wired and disposed for upload/download/pagination/search endpoints
- Certificate pinning confirmed for any government/military endpoint
- No duplicated endpoints
- No breaking changes
- Tests added for the Api/Repository layer, including the error-mapping path
