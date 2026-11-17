---
name: flutter-create-crud
description: Add Create, Read, Update and Delete operations to an existing feature while preserving the project architecture.
paths:
  - "security_mobile/**/*.dart"
  - "admin_web/**/*.dart"
  - "security_mobile/**/pubspec.yaml"
  - "admin_web/**/pubspec.yaml"
  - "security_mobile/**/pubspec.lock"
  - "admin_web/**/pubspec.lock"
---

# Goal
Implement the required CRUD operations for an existing feature.
Generate only the missing components.
Reuse existing implementations whenever possible.
Follow all project Rules automatically.
---
# When To Use
Use this skill when
- implementing CRUD operations
- extending an existing feature
- completing a partially implemented feature
Do not use this skill to create a new feature.
---
# Execution Principles
- Inspect before generating.
- Reuse before creating.
- Extend before replacing.
- Generate only missing operations.
- Preserve existing architecture.
- Follow project Rules.
- Cross-check the cross-cutting invariants and Definition of Done in the `flutter-senior-engineer` skill before finishing — don't re-derive or re-list them here.
---
# Required Inputs
Inspect the project first.
Collect only missing information.
Examples
- Feature name
- Required operations
- API availability
- Read-only or Editable
---
# Workflow
## Step 1 — Inspect
Locate the feature.
Identify
- Entity
- Repository
- DTOs
- Api
- Presentation controller (ViewModel or Cubit)
---
## Step 2 — Analyze
Determine which CRUD operations already exist.
Generate only the missing ones.
For each missing operation, note its HTTP method — it decides the retry policy in Step 3.
---
## Step 3 — Generate
Update only the required files.
Possible updates
- DTO
- Mapper
- Api
- DataSource
- Repository
- UseCases
- Presentation controller (ViewModel or Cubit)
- Forms
- Pages

Retry policy follows HTTP method, matching every other integration in this project: Read (GET) is retryable via `executeWithRetry()`; Update (PUT) only if the endpoint is confirmed idempotent server-side; Create/Delete (POST/DELETE) are never auto-retried without an explicit idempotency key.

Create and Update operations that update the UI before the server confirms follow the project's optimistic-update flow: persist locally → update UI → send remote request → roll back deterministically on failure. The rollback must restore the exact prior state, not just "refresh from server" — a refresh can silently mask a rollback bug.

Delete requires the user to confirm before the operation runs. Use whatever confirmation UI is already established in this project for destructive actions — check the existing codebase for the pattern in use rather than assuming one. Do not skip confirmation for delete regardless of which UI component ends up presenting it.
---
## Step 4 — Integrate
Connect with
- Riverpod or BLoC (the project's resolved stack)
- Error Pipeline
- Localization
- Routing (if required)

Forms follow the project's existing form rule: a plain controller for a single field, the project's form-group solution once three or more related fields or cross-field validation are involved — don't introduce a new form pattern for this feature.
---
## Step 5 — Test
Add tests for each new or changed operation at the Repository level, including:
- the success path
- the failure path, verifying it surfaces the correct Failure type through the existing error pipeline
- for Create/Update using optimistic updates: a test that forces the remote call to fail and confirms the rollback restores the correct prior state

A CRUD operation without a Repository-level test for both its success and failure path is not complete.
---
## Step 6 — Review
Ensure the CRUD implementation is complete.
Remove duplicated code.
Reuse existing infrastructure.
Confirm against `flutter-senior-engineer`'s Definition of Done for the layers touched.
---
# Expected Output
A complete CRUD implementation.
No duplicate files.
No architectural violations.
Only necessary files are changed.
---
# Self Review
Before finishing verify
- CRUD operations complete
- Repository updated
- Presentation controller (ViewModel or Cubit) updated
- DTO mapped correctly
- Error handling preserved
- Retry behavior matches HTTP method (Create/Delete never auto-retried without an idempotency key)
- Optimistic updates (where used) roll back to the exact prior state on failure, verified by a test
- Delete requires explicit user confirmation before executing
- No duplicated logic
- No unused files
- Tests exist for each operation's success and failure path
