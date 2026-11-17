---
name: flutter-refactor-feature
description: Improve an existing feature while preserving behavior and project architecture.
paths:
  - "security_mobile/**/*.dart"
  - "admin_web/**/*.dart"
  - "security_mobile/**/pubspec.yaml"
  - "admin_web/**/pubspec.yaml"
  - "security_mobile/**/pubspec.lock"
  - "admin_web/**/pubspec.lock"
---

# Goal
Refactor code safely.
Improve maintainability.
Preserve behavior.
Follow all project Rules.
---
# When To Use
Use this skill when
- reducing technical debt
- improving readability
- simplifying architecture
- removing duplication
Do not use this skill for adding new functionality.
---
# Execution Principles
- Inspect before modifying.
- Preserve public behavior.
- Refactor incrementally.
- Minimize changes.
- Follow project Rules.
- Cross-check the cross-cutting invariants and Definition of Done in the `flutter-senior-engineer` skill before finishing.
---
# Required Inputs
Inspect the project first.
Determine the scope of refactoring.
---
# Workflow
## Step 1 — Inspect
Understand the feature.
Locate architectural issues.

Check what test coverage already exists for this feature. If there are no tests for the code being touched, that's not a reason to skip the baseline step below — it's a reason to write minimal characterization tests first, covering current behavior, before changing anything. Refactoring untested code without first capturing its current behavior is not "refactoring," it's a rewrite with no safety net.
---
## Step 2 — Baseline
Run the existing tests (or the characterization tests just added) and record that they pass before any change is made.

This baseline is what "preserve behavior" is checked against in Step 4 — not a feeling that the code "looks the same," an actual pass/fail comparison.
---
## Step 3 — Analyze
Identify issues using the project's own numeric thresholds from `general-best-practices.mdc`, not general impressions:

- duplication
- tight coupling
- a method over the ~25-line soft limit
- a class matching the God Object criteria (more than ~7 injected dependencies, crosses layer boundaries, or can't be described in one sentence without "and")
- misplaced responsibilities relative to the layer it's in (per `architecture.mdc`)

"This class feels large" is not sufficient justification on its own — tie the refactor to the specific threshold or rule being violated.
---
## Step 4 — Refactor
Apply the smallest safe changes.
Reuse existing code.
Avoid unnecessary abstractions.

Scope limit: one structural change per refactor pass (e.g. "extract this UseCase" or "split this God Object into UseCase+ViewModel+Mapper" — not both in the same pass). Multiple unrelated structural changes bundled together make the diff unreviewable and the rollback point unclear if something breaks.

If the refactor changes an architectural pattern rather than just cleaning up code within the existing pattern (e.g. splitting a class along new lines, changing how a layer talks to another), that's an architecture decision — use the `flutter-write-adr` skill to record it, don't let it pass as a routine cleanup.

Any new class produced by the refactor (an extracted Mapper, a split ViewModel) follows `naming-conventions.mdc` exactly — a refactor is not exempt from naming rules just because nothing "new" was conceptually added.
---
## Step 5 — Review
Verify
- Architecture improved
- Behavior preserved — confirmed by re-running the Step 2 baseline tests, not assumed
- Complexity reduced, measured against the specific threshold that triggered the refactor (method now under the line limit, dependency count now under the God Object threshold, etc.)
- Naming conventions respected for any new class
Confirm against `flutter-senior-engineer`'s Definition of Done for the layers touched.
---
# Expected Output
Cleaner code.
Improved maintainability.
No behavioral changes, verified by tests passing before and after.
---
# Success Criteria
The task is complete only if
- Behavior unchanged, confirmed by the Step 2 baseline passing again after the refactor
- Complexity reduced against a specific, named threshold — not a general impression
- Rules respected
- No duplicated logic introduced
- Exactly one structural change per pass; anything larger is split into a follow-up pass or recorded as an ADR
---
# Self Review
Before finishing verify
- No regressions introduced — baseline tests (existing or newly added characterization tests) pass both before and after
- Feature remains isolated
- Architecture improved, tied to a specific threshold from `general-best-practices.mdc` or `architecture.mdc`
- Code simplified
- Any new class named per `naming-conventions.mdc`
- A pattern-level change (not just internal cleanup) has a corresponding ADR
