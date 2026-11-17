---
name: flutter-review-feature
description: Review one existing Flutter feature against project rules and flutter-senior-engineer. Use when reviewing a feature, a pull request scoped to one module, or when the Review Feature command runs. Do not rewrite code unless the user asked.
paths:
  - "security_mobile/**/*.dart"
  - "admin_web/**/*.dart"
  - "security_mobile/**/pubspec.yaml"
  - "admin_web/**/pubspec.yaml"
  - "security_mobile/**/pubspec.lock"
  - "admin_web/**/pubspec.lock"
---

# Goal

Produce a structured review of **one** feature. Identify architecture violations. Prioritize findings. Recommend the smallest safe fixes.

Do not rewrite code unless explicitly requested.

This skill does not own review criteria. Apply `flutter-senior-engineer` (Definition of Done + eight invariants) and the project Rules.

---

# When To Use

Use when reviewing a completed feature, a feature-scoped PR, or an audit of one module.

Do not use this skill to review the entire repository — use Review Flutter Project.

Do not use this skill to create or refactor the feature.

---

# Required Inputs

Inspect the feature. Infer purpose from code.

If the feature was produced by another project skill, verify that skill's Self Review was actually satisfied — trust but verify.

---

# Workflow

## Step 1 — Inspect

Map Domain, Data, Presentation, providers, routes, and tests for this feature only.

## Step 2 — Review

Walk `flutter-senior-engineer` Definition of Done for every layer this feature has.

Then check the eight cross-cutting invariants one by one.

## Step 3 — Prioritize

Classify each finding: Critical, High, Medium, Low.

A cross-cutting invariant violation is never Low.

## Step 4 — Recommend

Smallest safe fix per finding. Name the rule file (and invariant number when relevant). Do not propose unrelated refactors.

## Step 5 — Report

```markdown
# Feature review: <name>

## Strengths
- …

## Findings
| Severity | Finding | Rule / invariant | Fix |
|---|---|---|---|
| Critical | … | … | … |

## Producing skill Self Review
Satisfied / not satisfied / unknown — one line.

## Next actions
1. …
```

---

# Expected Output

A report. No code changes unless the user asked to apply fixes.

---

# Self Review

- Only this feature was reviewed
- All eight invariants checked
- Findings tied to a rule or invariant
- No rewrite performed unless requested
