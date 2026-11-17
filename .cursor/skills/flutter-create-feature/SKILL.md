---
name: flutter-create-feature
description: Create a complete Flutter feature that follows the project's architecture and engineering standards.
paths:
  - "security_mobile/**/*.dart"
  - "admin_web/**/*.dart"
  - "security_mobile/**/pubspec.yaml"
  - "admin_web/**/pubspec.yaml"
  - "security_mobile/**/pubspec.lock"
  - "admin_web/**/pubspec.lock"
---

# Goal
Create a production-ready Flutter feature.
The feature must integrate seamlessly into the existing project.
Follow all project Rules automatically.
Do not invent a different architecture.
Cross-check the cross-cutting invariants and Definition of Done in the `flutter-senior-engineer` skill before finishing.
---
# When To Use
Use this skill when
- creating a new feature
- implementing a new business module
- starting a new screen
- adding a new domain
Do not use this skill to modify an existing feature.
Use the appropriate maintenance skill instead.
---
# Required Inputs
Before generating code, inspect the project.
Collect only the missing information.
Required information:
- Feature name
- Business purpose
Optional information (only if needed):
- State stack (`riverpod` or `bloc`) — only if `.cursor/docs/flutter/stack.md` is `auto` and the app does not already use one
- API endpoint
- CRUD or Read-only
- Requires authentication
- Requires routing
- Requires form
- Requires offline support
- Touches a government/military endpoint or displays classified/PII data
Never ask questions whose answers can be inferred from the project.
---
# Workflow
## Step 1 — Inspect
Understand the existing project.
Identify:
- architecture
- folder structure
- shared infrastructure
- reusable components
- existing base classes
Reuse existing implementations whenever possible.
---
## Step 2 — Plan
Determine which layers are required.
Only generate files that are actually needed.
Avoid unnecessary abstractions.
Avoid empty files.
---
## Step 3 — Build Domain
Create the Domain layer first.
Generate only the required components.
Examples:
- Entity
- Repository Contract
- UseCases (`.cursor/templates/flutter/12-usecase.md`)
Domain must not depend on Flutter.
---
## Step 4 — Build Data
Generate the Data layer.
Possible components:
- DTO
- Mapper
- Api
- RemoteDataSource
- LocalDataSource (`.cursor/templates/flutter/13-local_datasource.md`) — only if cache/offline is in scope
- Repository Implementation
Reuse existing infrastructure.
Never duplicate networking code.
If the feature touches a government/military endpoint, certificate pinning must already be configured for it in `core/network/` before this layer is considered done — this is a blocker, not a follow-up item.
---
## Step 5 — Build Presentation
Generate the Presentation layer.
Possible components:
- Page
- Presentation controller — Riverpod ViewModel **or** BLoC Cubit (`.cursor/templates/flutter/8-viewmodel.md`)
- Widgets
- Form
Keep widgets small.
Business logic belongs outside the UI.
Every user-facing string added in this step gets a key in both `app_en.arb` and `app_ar.arb` — never add a key to one and forget the other, and never leave a string hardcoded "for now."
---
## Step 6 — Integrate
Integrate the feature with existing infrastructure.
Examples:
- Riverpod or flutter_bloc (the resolved stack only)
- GoRouter
- Localization
- Error Handling
- Design System
Only integrate what is required.
---
## Step 7 — Security Pass
Before review, check whether this feature needs any of the following — do not skip this step by assuming "probably not":
- Secure storage (`flutter_secure_storage`) for any token, session, or license data introduced
- Certificate pinning for any new government/military endpoint
- Screenshot/screen-recording prevention if the screen shows classified, licensed, or personally identifiable data
- Session auto-lock interaction if the screen exposes sensitive data behind an inactivity-sensitive flow
Skip only what genuinely doesn't apply — don't skip by default.
---
## Step 8 — Test
Write tests before considering the feature complete, in this order: UseCase → Repository → Mapper → presentation controller, then Widget tests only if the widget has meaningful logic beyond rendering (`.cursor/templates/flutter/14-tests.md`).
Mock external dependencies only — never mock the business rule being tested.
A feature without UseCase/Repository tests is not done, even if it compiles and runs.
---
## Step 9 — Review
Before finishing, perform a self-review against every item in Self Review below.
Fix issues before returning the result.
---
# Expected Output
The generated feature should
- compile without architectural violations
- follow the project structure
- reuse existing infrastructure
- avoid duplicated code
- include only necessary files
Do not generate placeholder code unless information is genuinely unavailable.
---
# Self Review
Before finishing, verify:

**Architecture**
- Feature is isolated
- Layers are separated
- No Business Logic inside UI
- No direct Widget → Repository access
- DTO never leaves the Data layer
- Entity is used outside Data
- Presentation controller integration is correct for the resolved stack (Riverpod **or** BLoC, never both)
- Routing is integrated when required
- Localization is respected — every new string exists in both `app_en.arb` and `app_ar.arb`, no orphaned key on either side
- Error handling follows the project pipeline

**Naming**
- Every new Failure type (if any) uses the canonical `HttpErrorFailure` spelling — never `HttpFailure` — and is one of the eleven types already defined, not a new ad hoc one
- The `my_` file/class prefix is used only for the four shared design-system widgets — never applied to a feature-specific widget
- The screen has one controller: Riverpod ViewModel **is** its Notifier/AsyncNotifier, **or** BLoC Cubit/Bloc is the controller — never two classes

**Security**
- Secrets are not hardcoded; sensitive data goes through `flutter_secure_storage`
- Government/military endpoints have certificate pinning configured, not planned
- Screenshot prevention and session auto-lock applied wherever this feature's data classification requires them

**Hygiene**
- No duplicated implementations
- No unused files
- No TODO placeholders
- UseCase/Repository/Mapper tests exist and pass

Walk `flutter-senior-engineer` Definition of Done and the eight invariants. Return the final result only after all checks pass.
