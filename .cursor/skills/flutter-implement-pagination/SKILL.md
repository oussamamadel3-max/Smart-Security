---
name: flutter-implement-pagination
description: Add pagination to an existing feature while preserving the project architecture.
paths:
  - "security_mobile/**/*.dart"
  - "admin_web/**/*.dart"
  - "security_mobile/**/pubspec.yaml"
  - "admin_web/**/pubspec.yaml"
  - "security_mobile/**/pubspec.lock"
  - "admin_web/**/pubspec.lock"
---

# Goal
Implement efficient pagination.
Reuse the existing feature.
Avoid unnecessary refactoring.
Follow all project Rules.
---
# When To Use
Use this skill when
- loading large datasets
- implementing infinite scrolling
- adding server pagination
- replacing full data loading
---
# Execution Principles
- Inspect before generating.
- Reuse before creating.
- Preserve architecture.
- Generate only required changes.
- Cross-check the cross-cutting invariants and Definition of Done in the `flutter-senior-engineer` skill before finishing — the CancelToken invariant below is one of them.
---
# Required Inputs
Collect only missing information.
Examples
- Pagination type
- API contract
- Page size
---
# Workflow
## Step 1 — Inspect
Locate
- Repository
- Api
- Presentation controller (ViewModel or Cubit)
- Existing lists
---
## Step 2 — Analyze
Determine pagination strategy.
Examples
- Page
- Offset
- Cursor

Determine the controller type: pagination is CRUD-like state (loading more, a single page failing independently, retry for just that page) — Riverpod: AsyncNotifier, not FutureProvider. BLoC: Cubit (or Bloc), not a one-shot Future in the Page. The mutation (loading page N+1) is what decides it (`state-management.mdc`).
---
## Step 3 — Integrate
Update only
- Api
- Repository
- Presentation controller (ViewModel or Cubit)
Reuse existing DTOs whenever possible.

Every page request gets a `CancelToken`, released on controller dispose (`ref.onDispose()` or Cubit/Bloc `close()`) — pagination is one of the categories this project's cancellation rule explicitly targets. This matters here specifically because of the race it prevents: if the user scrolls fast or changes a filter mid-load, an in-flight request for a now-stale page/filter combination must not be allowed to land and corrupt the list — cancel it instead of letting it resolve into state nobody wants anymore.

Retry for a page load follows the project's GET retry policy (`executeWithRetry()`) — pagination requests are reads, so this applies without the POST/DELETE caveats.
---
## Step 4 — Preserve
Preserve
- Existing filters
- Search
- Sorting

When a filter or sort changes, the current page state resets and any in-flight page request for the old filter is cancelled (see Step 3) — don't append new-filter results onto an old-filter list.
---
## Step 5 — Test
Cover:
- loading page 2 while page 1 is still displayed appends correctly, no duplicates
- rapid successive scroll-triggered loads don't produce duplicate or out-of-order pages
- changing a filter mid-load cancels the stale request and the list reflects only the new filter's results
- a single page's failure surfaces a retry option for that page without resetting pages already loaded

Pagination without a test for "filter changed while a page was loading" is not complete — that's the race condition this skill exists to prevent, so it's the one case that must be verified, not assumed handled.
---
## Step 6 — Review
Verify
- No duplicated items
- No duplicated requests
- Scroll preserved
- Retry supported, GET-policy, per page
- Stale requests (old filter/page) are cancelled, not merely ignored on arrival
Confirm against `flutter-senior-engineer`'s Definition of Done for the layers touched.
---
# Expected Output
Efficient pagination.
Minimal code changes.
No architecture violations.
---
# Success Criteria
The task is complete only if
- Duplicate requests prevented
- Duplicate items prevented
- Existing behavior preserved
- Performance improved
- Stale in-flight requests are cancelled on filter/sort change, not just superseded
---
# Self Review
Before finishing verify
- Pagination works
- Existing filters preserved
- Repository updated
- Presentation controller updated (AsyncNotifier or Cubit)
- CancelToken wired per page request and released on dispose (`ref.onDispose()` or `close()`)
- Retry follows `executeWithRetry()` for page loads
- Tests cover duplicate prevention, rapid scroll, and filter-change-mid-load cancellation
