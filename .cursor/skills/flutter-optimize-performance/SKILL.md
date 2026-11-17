---
name: flutter-optimize-performance
description: Analyze and optimize an existing Flutter feature while preserving behavior and architecture.
paths:
  - "security_mobile/**/*.dart"
  - "admin_web/**/*.dart"
  - "security_mobile/**/pubspec.yaml"
  - "admin_web/**/pubspec.yaml"
  - "security_mobile/**/pubspec.lock"
  - "admin_web/**/pubspec.lock"
---

# Goal
Improve runtime performance.
Reduce unnecessary rebuilds.
Optimize rendering, memory usage and asynchronous operations.
Preserve the existing functionality.
Follow all project Rules.
---
# When To Use
Use this skill when
- performance issues appear
- screens become slow
- scrolling is not smooth
- startup becomes slower
- memory usage increases
Do not use this skill to introduce new functionality.
---
# Execution Principles
- Measure before optimizing — with DevTools, not by reading code and guessing what looks slow.
- Optimize the bottleneck the measurement actually identified, not the one that seems most likely.
- Preserve architecture.
- Avoid premature optimization.
- Minimize code changes.
- Cross-check the cross-cutting invariants and Definition of Done in the `flutter-senior-engineer` skill before finishing.
---
# Required Inputs
Inspect the project first.
Collect only missing information.
Examples
- Slow screen
- Large list
- Heavy API
- Expensive rebuilds
- Reported symptom: janky scrolling, slow startup, growing memory, or a specific interaction that feels slow — this decides which DevTools view to open in Step 1
---
# Workflow
## Step 1 — Measure (before touching any code)

Match the symptom to the tool, per `performance.mdc` — don't default to Performance view for everything:

- Janky scrolling / dropped frames / slow interaction → DevTools **Performance** view; capture frame build/raster times for the specific interaction
- Growing memory / suspected leak on a long-lived screen → DevTools **Memory** view; check heap growth across repeated navigation in/out of the screen
- Slow cold start → time-to-interactive measurement, and check what's running before first frame (per the App Startup section of `performance.mdc`)
- Large app size growth → `flutter build --analyze-size`

Record the baseline numbers (frame time, heap size, startup time — whichever applies) before any change. This is what Step 4 compares against; without it there's no way to know if the change actually helped.
---
## Step 2 — Detect

Using the Step 1 trace, not code inspection alone, identify

- unnecessary rebuilds (Performance view: which widgets rebuild on the traced interaction, and whether their state actually changed)
- expensive computations (a build/raster time spike that lines up with a specific synchronous call)
- duplicated requests
- blocking operations (any synchronous work over performance.mdc's ~16ms threshold on the main isolate)
- memory leaks (Memory view: heap that doesn't return to baseline after navigating away)

If the trace doesn't clearly point to one of these, that's a signal to profile a narrower interaction, not to guess.
---
## Step 3 — Optimize

Apply the minimum required improvements.
Reuse existing architecture.

Fixes map to what performance.mdc already prescribes for the category found in Step 2 — const constructors and `select()`/rebuild-scope narrowing for excess rebuilds, `compute()`/`Isolate.spawn()` for blocking sync work, `cached_network_image`/`cacheWidth`/`cacheHeight` for image cost, pagination/windowing for large lists. Don't invent a new pattern when an existing rule already covers the fix.
---
## Step 4 — Verify (after)

Re-run the same DevTools measurement from Step 1, on the same interaction, and compare against the recorded baseline.

Verify that

- behavior is unchanged
- readability is preserved
- complexity is not increased
- the measured number actually improved — a change with no measurable before/after difference is not a verified optimization, revert it or find the real bottleneck

Check the result against the project's numeric targets where relevant: 60 FPS / no frame over 16ms, cold start under 2s. Hitting the target isn't required for every single fix (some bottlenecks are relative improvements toward it), but a fix claimed to address FPS or startup should visibly move the needle toward that number, not just "feel snappier."
---
## Step 5 — Test

Confirm no functional regression: the existing test suite for this feature still passes, and if the optimization touched async/isolate boundaries (moved work to `compute()`, changed a Provider's disposal), add a test for the specific behavior that changed shape even if the output value is the same — e.g. that a value returned via `compute()` still completes and propagates correctly on error.
---
## Step 6 — Review

Verify that
- behavior is unchanged
- readability is preserved
- complexity is not increased
- Step 4's before/after measurement is recorded, not just claimed

Confirm against `flutter-senior-engineer`'s Definition of Done for the layers touched.
---
# Expected Output
Improved performance, with a recorded before/after measurement.
No architectural violations.
No behavioral changes.
---
# Success Criteria
The task is complete only if
- Performance improved, confirmed by a DevTools measurement, not assumed
- Existing behavior preserved, confirmed by tests passing
- Rules respected
- No unnecessary complexity added
---
# Self Review
Before finishing verify
- A DevTools baseline was captured before any change, matched to the reported symptom
- Widget rebuilds reduced, confirmed by the trace, not just by removing code that "looked expensive"
- Expensive operations optimized using the pattern `performance.mdc` already prescribes for that category
- Async work improved (moved off-main-isolate if it exceeded the 16ms threshold)
- Before/after numbers are both recorded and the after number is actually better
- No regressions introduced — existing tests pass, new tests added if async/lifecycle behavior changed shape
