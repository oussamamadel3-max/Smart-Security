# Repository Implementation Template

## Purpose

Implement the Repository Contract.

Coordinate communication between data sources and the Domain layer.

---

## Responsibilities

- Retrieve data.
- Persist data.
- Convert DTOs into Entities.
- Return business results.

---

## Structure

Generate:

- Repository implementation
- Dependency injection
- Mapper usage
- Error handling integration

---

## Dependencies

Allowed

- Repository Contract
- Data Sources
- Mapper
- Result

Forbidden

- UI
- Widgets
- BuildContext

---

## Rules

- Never expose DTOs.
- Never return API responses.
- Never contain UI logic.

---

## Output Expectations

The Repository Implementation must

- satisfy the Repository Contract
- hide infrastructure details
- return business models only