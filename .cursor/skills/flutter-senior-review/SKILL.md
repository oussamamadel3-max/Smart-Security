---
name: flutter-senior-review
description: Perform a complete engineering review of a Flutter feature using the project's architecture and standards.
paths:
  - "security_mobile/**/*.dart"
  - "admin_web/**/*.dart"
  - "security_mobile/**/pubspec.yaml"
  - "admin_web/**/pubspec.yaml"
  - "security_mobile/**/pubspec.lock"
  - "admin_web/**/pubspec.lock"
---

# Goal
Review the feature as a Senior Flutter Engineer.
Identify architectural, structural and quality issues.
Prioritize findings.
Suggest the minimum safe improvements.
Do not rewrite code unless explicitly requested.
---
# When To Use
Use this skill when
- reviewing completed work
- reviewing pull requests
- auditing an existing feature
- preparing a release
---
# Execution Principles
- Review before suggesting.
- Explain every finding.
- Prioritize issues.
- Preserve project architecture.
- Follow all project Rules.
- This skill does not maintain its own review criteria — it applies the Definition of Done and cross-cutting invariants already defined in the `flutter-senior-engineer` skill. If those criteria change, this review changes with them automatically; don't fork a separate checklist here.
---
# Required Inputs
Inspect the feature.
Understand its purpose.
Do not request unnecessary information.

Also determine: was this feature built using one of the project's other skills (Create Flutter Feature, Integrate REST API, Create CRUD Operations, etc.)? If so, the review can focus on confirming the skill's own Self Review was actually satisfied, rather than re-deriving every finding from scratch — trust but verify, don't redo the full analysis blind.
---
# Workflow
## Step 1 — Inspect
Understand
- Feature purpose
- Data flow
- Dependencies
- Which skill (if any) produced this code, per Required Inputs
---
## Step 2 — Review

Review every layer against `flutter-senior-engineer`'s Definition of Done, section by section — Layering, State & data, Errors, Security, Presentation, Naming, Tests. Don't substitute a general impression of "Architecture looks fine" for checking the actual line items.

Then check the 8 cross-cutting invariants from `flutter-senior-engineer` explicitly, one by one — these are the findings most likely to be missed by a layer-by-layer read, because each one only shows up when two layers are read together:

1. `HttpErrorFailure` spelled correctly everywhere, never `HttpFailure`
2. Retry wording/behavior is HTTP-method-based and consistent with `network-architecture.mdc`/`repository.mdc`/`error-handling.mdc`
3. Every screen-scoped `CancelToken` is paired with controller dispose (`ref.onDispose()` or `close()`)
4. Unauthorized navigation has exactly one path: `authGuardRedirect`, nothing ad hoc elsewhere
5. Certificate pinning is actually configured for any government/military endpoint touched, not just planned
6. One presentation controller per screen (ViewModel **is** Notifier, **or** Cubit/Bloc is the controller — never two classes, never both stacks)
7. `AppTheme`/`AppColors`/`AppTypography`/`AppSemanticColors` and `My*` widgets are the project's actual systems, not Flutter defaults or ad hoc values
8. Locale access goes through `context.tr` and the stack's locale owner, never `AppLocalizations.of(context)` directly

A feature that passes every individual layer's rules but fails one of these 8 is still a finding — flag it as such, don't let per-layer cleanliness excuse a cross-cutting miss.
---
## Step 3 — Release Readiness (only when "preparing a release" is the stated purpose)

Check specifically, separately from the general review:

- Obfuscation (`--obfuscate`) enabled for the release build
- Certificate pinning configured for every government/military endpoint in this feature, not deferred
- Debug logging disabled in the release configuration; no passwords/tokens/personal data/GPS coordinates/government IDs logged even at debug level
- Device integrity checks (root/jailbreak detection) and session auto-lock active if `security.mdc` requires them for this feature's data classification
- Screenshot/screen-recording prevention applied to any screen showing classified, licensed, or PII data

Skip this step entirely for a routine feature or PR review — it's specific to release preparation, not a general-purpose addition to every review.
---
## Step 4 — Prioritize
Classify findings
- Critical
- High
- Medium
- Low

A cross-cutting invariant violation (Step 2) or a release-readiness item (Step 3) is never Low — these are the categories designed to catch what looks fine layer-by-layer but breaks in production or ships insecure.
---
## Step 5 — Recommend
Suggest the smallest safe improvements.
Do not propose unnecessary refactoring.
For anything Critical or High, name the specific rule file and line item violated — not just "this could be cleaner."
---
## Step 6 — Report
Summarize
- strengths
- weaknesses
- priorities
- next actions
- (if a producing skill was identified in Step 1) whether that skill's own Self Review was actually satisfied, as a distinct line — this is useful signal for whether the skill itself needs tightening, separate from this specific feature's issues
---
# Expected Output
A structured engineering review.
Clear priorities.
Actionable recommendations.
---
# Success Criteria
The task is complete only if
- Every applicable layer reviewed against `flutter-senior-engineer`'s Definition of Done, not a general impression
- All 8 cross-cutting invariants checked explicitly
- Release readiness checked when that's the stated purpose
- Findings prioritized
- Recommendations actionable
- Architecture preserved
---
# Self Review
Before finishing verify
- Findings explained, each tied to a specific rule file/line item where applicable
- All 8 cross-cutting invariants checked, not skipped as "probably fine"
- Release readiness checked if this was a release review
- Priorities assigned, with cross-cutting/release findings never marked Low
- No unsupported assumptions made
- Recommendations consistent with project Rules
