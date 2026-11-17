---
name: flutter-write-adr
description: Write an Architecture Decision Record when a refactor or feature changes how layers communicate, storage, routing, or auth — not for routine cleanup. Use when the user asks for an ADR or when the flutter-refactor-feature skill requires recording a pattern-level change.
paths:
  - "security_mobile/**/*.dart"
  - "admin_web/**/*.dart"
  - "security_mobile/**/pubspec.yaml"
  - "admin_web/**/pubspec.yaml"
  - "security_mobile/**/pubspec.lock"
  - "admin_web/**/pubspec.lock"
---

# Goal

Record a pattern-level architecture decision in the Flutter app repo so later work does not reverse it silently.

Do not write an ADR for renaming, extracting a method, or formatting.

---

# When To Use

Use when

- a refactor changes how two layers talk
- storage, routing, auth, or offline sync strategy changes
- the flutter-refactor-feature skill says the change is architectural
- the user asks to record a decision

Do not use for routine cleanup inside an existing pattern.

---

# Location

Create the file in the **Flutter application** repository, not in `.cursor`:

```text
docs/adr/NNNN-short-title.md
```

`NNNN` is the next integer in that folder, zero-padded to four digits. If `docs/adr/` does not exist, create it. Title slug is lowercase kebab-case.

---

# Workflow

## Step 1 — Confirm it is architectural

If the change only moves code within the same pattern, stop. No ADR.

## Step 2 — Number

List `docs/adr/`. Next number = max existing + 1. Start at `0001` if the folder is empty.

## Step 3 — Write

Use this template, filled in. Do not leave angle-bracket placeholders.

```markdown
# NNNN. Title

Date: YYYY-MM-DD

Status: Accepted

## Context

What problem forced a choice? What constraints (rules, security, existing code) apply?

## Decision

What we will do, in one short paragraph.

## Alternatives

- Option A — why rejected
- Option B — why rejected

## Consequences

What becomes easier, what becomes harder, which files/layers own the new pattern.

## Follow-up

Any required code or rule updates. "None" if complete.
```

## Step 4 — Link

Mention the ADR path in the PR/summary of the refactor. Do not duplicate the decision inside a skill.

---

# Self Review

- File is under `docs/adr/` in the Flutter app
- Number does not collide
- Status is Accepted (or Proposed only if the user asked not to commit yet)
- Context explains the choice; Decision is not a restatement of Context
