# Local Data Source Template

## Purpose

Persist and read feature data on device. Used when offline cache is in scope.

The local source belongs to the Data layer. It never talks to the UI.

---

## Responsibilities

- Read/write cache models (not Entities).
- Use Isar unless the project already has a different local store in `lib/core/`.
- Isolate persistence details from the repository.

---

## Structure

Generate only if the feature needs cache or offline writes:

- `FeatureLocalDataSource`
- Methods aligned with the repository operations that are cached
- Clear/invalidate helpers the repository actually calls

Do not generate an empty local source "for later".

---

## Dependencies

Allowed

- Isar (or the project's existing local store)
- Cache / DTO-like models that stay in Data

Forbidden

- Flutter widgets
- Entity (map in the repository / mapper)
- Dio
- ViewModel

---

## Rules

- Never become a second source of truth. The repository decides when to read local vs remote.
- Offline write queues must be persisted (survive process death), not held only in memory.
- Do not introduce SharedPreferences for feature payloads. Tokens stay in `SessionStorage`; feature cache stays in Isar.

---

## Example

```dart
class CoursesLocalDataSource {
  Future<List<CourseCacheDto>> getAll();
  Future<void> upsertAll(List<CourseCacheDto> items);
  Future<void> enqueueWrite(CourseWriteOp op);
  Future<List<CourseWriteOp>> pendingWrites();
}
```

---

## Output Expectations

The local source is reusable, Isar-backed, and invisible to Presentation.
