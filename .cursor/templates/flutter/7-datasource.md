# Remote Data Source Template

## Purpose

Coordinate communication between the API layer and the Repository.

---

## Responsibilities

- Call API methods.
- Return DTOs.
- Isolate networking details.

---

## Structure

Generate:

- Remote data source
- Feature-specific methods

---

## Dependencies

Allowed

- API
- DTO

Forbidden

- UI
- Entity
- BuildContext

---

## Rules

- Never perform business logic.
- Never return Entities.
- Never access Presentation.

---

## Output Expectations

The Data Source must

- provide clean access to remote data
- remain reusable