# UseCase Template

## Purpose

Encode one domain operation. The UseCase belongs to the Domain layer.

Presentation controllers call UseCases. UseCases call the repository contract. Nothing else.

---

## Responsibilities

- Expose a single `call(...)` operation.
- Return `Result<T>`.
- Stay free of Flutter, Dio, DTO, and UI.

---

## Structure

Generate:

- One class per operation (`LoginUseCase`, `GetCourseUseCase`)
- Constructor injection of the repository contract
- `call` with named parameters matching the business operation

Do not generate a UseCase that only forwards a getter with no rule if the presentation controller can use an existing UseCase. Prefer real operations (login, logout, getCurrentUser).

---

## Dependencies

Allowed

- Repository contract
- Entity
- Result

Forbidden

- Flutter
- Dio
- DTO
- DataSource
- BuildContext

---

## Rules

- Never call APIs.
- Never map DTOs.
- Never hold UI state.
- One operation per class.

---

## Example

```dart
class GetCourseUseCase {
  final CoursesRepository _repository;
  GetCourseUseCase(this._repository);

  Future<Result<CourseEntity>> call(String id) {
    return _repository.getById(id);
  }
}
```

---

## Output Expectations

The generated UseCase must compile without Flutter, return `Result<T>`, and be the only Domain entry the presentation controller uses for that operation.
