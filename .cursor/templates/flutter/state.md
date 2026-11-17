# State Template

## Purpose

Represent the complete UI state for a single feature.

The State acts as the single source of truth for the Presentation layer.

It should describe what the UI displays, not how business logic works.

---

## Responsibilities

- Represent screen state.
- Hold business data exposed to the UI.
- Represent loading states.
- Represent success states.
- Represent error states.
- Trigger UI updates through immutable state changes.

---

## Structure

Generate

- Immutable State
- Initial State
- Copy mechanism
- Required properties only

Keep the State focused on one feature.

Do not include unnecessary fields.

---

## Typical State

The generated State may include

- data
- loading status
- error state
- selected item
- search query
- filters
- pagination information

Generate only what the feature actually requires.

---

## Dependencies

Allowed

- Entity
- ErrorState

Forbidden

- DTO
- Repository
- Api
- Dio
- BuildContext
- Widgets

---

## Rules

The State should

- remain immutable
- represent UI state only
- avoid business logic
- avoid networking logic
- avoid serialization
- avoid validation logic

Never expose infrastructure objects.

Never expose Response objects.

Never expose DTOs.

---

## State Evolution

State changes should always represent a complete UI snapshot.

Avoid multiple independent mutable variables.

Keep related information together.

---

## Output Expectations

The generated State must

- be simple
- be predictable
- be immutable
- be easy to extend
- be suitable for the resolved stack (Riverpod Notifier state or BLoC/Cubit state)
- expose only information required by the UI