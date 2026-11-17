# Repository Contract Template

## Purpose

Define the business operations available for the feature.

The Repository Contract belongs to the Domain layer.

---

## Responsibilities

- Expose business operations.
- Hide implementation details.
- Abstract data sources.

---

## Structure

Generate:

- Abstract repository
- Feature-specific operations only

---

## Dependencies

Allowed

- Entity
- Result

Forbidden

- Dio
- DTO
- API
- Database
- Flutter

---

## Rules

- Never expose implementation details.
- Never return raw responses.
- Never depend on infrastructure.

---

## Output Expectations

The Repository Contract must

- define clear business operations
- remain infrastructure-independent