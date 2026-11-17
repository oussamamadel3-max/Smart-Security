---
name: flutter-add-offline-cache
description: Add offline-first support to an existing feature using the project's architecture.
paths:
  - "security_mobile/**/*.dart"
  - "admin_web/**/*.dart"
  - "security_mobile/**/pubspec.yaml"
  - "admin_web/**/pubspec.yaml"
  - "security_mobile/**/pubspec.lock"
  - "admin_web/**/pubspec.lock"
---

# Goal
Implement offline support without changing the feature behavior.
The Repository remains the single source of truth.
Follow all project Rules automatically.
---
# When To Use
Use this skill when
- adding offline support
- introducing local caching
- improving network resilience
- reducing unnecessary API calls
Do not use this skill to replace the existing architecture.
---
# Execution Principles
- Inspect before generating.
- Reuse before creating.
- Preserve Repository responsibilities.
- Follow Offline-First architecture.
- Minimize changes.
- Cross-check the cross-cutting invariants and Definition of Done in the `flutter-senior-engineer` skill before finishing.
---
# Required Inputs
Inspect the project first.
Collect only missing information.
Examples
- Feature name
- Cache strategy
- Local database (Isar unless the project specifies otherwise)
- Sync requirements

Ask explicitly which of the two scopes below this feature needs — don't assume. A read-only list screen and an editable form have very different implementation cost here, and building write-sync for a screen that only ever reads is wasted complexity nobody asked for.
---
# Scope: Read-Only Caching vs Full Offline-Write

**Read-only caching** — the feature only ever displays remote data, never edits it while offline.
Implement Step 1–4 (Inspect, Analyze, Integrate, Synchronize — read side only) and stop there. Skip the Write Sync step entirely.

**Full offline-write** — the feature lets the user create/edit/delete while offline.
Implement everything through the Write Sync step. This is materially more work (queue, conflict resolution, retry) — don't take this path unless the feature genuinely needs writes to work offline.

If unsure which one applies, ask rather than defaulting to the heavier option.
---
# Workflow
## Step 1 — Inspect
Locate
- Repository
- DataSource
- Entity
- DTO
- Presentation controller
Understand the existing data flow.
---
## Step 2 — Analyze
Determine
- Read flow
- Write flow (only if Full offline-write applies)
- Synchronization needs
- Cache lifetime
---
## Step 3 — Integrate
Update only the required components.
Possible updates
- LocalDataSource
- Repository
- Cache Models
- Sync Logic
Reuse existing infrastructure. Local persistence goes through Isar, matching the rest of the project — don't introduce a second local storage mechanism for this feature alone.
---
## Step 4 — Synchronize (Read)
Implement
Read
↓
Local Cache
↓
Remote Refresh
↓
Repository
↓
UI
Preserve a single source of truth.
---
## Step 5 — Write Sync (Full offline-write only — skip if Read-Only Caching)

Offline writes are queued, not applied-and-forgotten. The queue is persisted in Isar — not in-memory — so a pending write survives an app restart before it syncs.

Connectivity is monitored by the project's existing connectivity mechanism (check what's already wired before adding a new one); when connectivity returns, queued writes are retried automatically, in the order they were made, unless the project's existing sync logic already defines a different ordering (e.g. coalescing multiple edits to the same record into the latest one — check for this before assuming FIFO).

Conflict resolution stays inside the Repository, never in the presentation controller or UI. Decide and document which strategy applies for this feature specifically (last-write-wins, server-wins, or a merge) — don't leave it implicit.

Retry policy for queued writes follows the same HTTP-method rule as everywhere else in the project: GET-equivalents retry freely, POST/DELETE only with an explicit idempotency key, otherwise a failed write surfaces to the user rather than silently retrying forever.
---
## Step 6 — Test
Read-only caching: test that a cold read with no connectivity returns the last cached value, and that a foreground refresh updates the cache and notifies the UI.

Full offline-write, additionally test:
- a write made offline survives an app restart and is still queued after relaunch
- the write syncs automatically once connectivity returns
- a forced conflict (local and remote both changed) resolves per the strategy chosen in Step 5, not arbitrarily

Offline support without a test for "the app was killed while a write was queued" is not complete — that's the scenario most likely to lose data silently.
---
## Step 7 — Review
Ensure offline behavior is transparent to the UI.
Remove duplicated logic.
Confirm against `flutter-senior-engineer`'s Definition of Done for the layers touched.
---
# Expected Output
Offline support integrated, scoped to what the feature actually needs (read-only or full write).
Repository controls synchronization.
UI remains unchanged.
---
# Success Criteria
The task is complete only if
- Repository remains the source of truth.
- UI is unaware of cache implementation.
- No duplicated synchronization logic.
- Existing behavior is preserved.
- For full offline-write: the queue survives app restart and the conflict strategy is explicit, not implicit.
---
# Self Review
Before finishing verify
- Local cache integrated, using Isar
- Repository updated
- Synchronization implemented (read-only, or read+write per the scope chosen)
- Existing APIs reused
- No duplicated code
- (Full offline-write only) Queue is persisted, retry follows the project's HTTP-method retry policy, conflict resolution strategy is explicit and lives in the Repository
- Tests cover the cold-read case, and — for full offline-write — restart-survival, auto-sync-on-reconnect, and conflict resolution
