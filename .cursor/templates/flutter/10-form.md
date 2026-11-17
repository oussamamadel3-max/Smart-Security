# Form Template

## Purpose

Manage user input for a feature.

The Form is responsible only for collecting and validating input.

---

## Responsibilities

- Define fields.
- Validate input.
- Expose form state.
- Submit user input.

---

## Structure

Generate

- Form model
- Validation rules
- Submit action
- Error display integration

---

## Dependencies

Allowed

- Reactive Forms
- Validation utilities

Forbidden

- Repository
- API
- DTO
- Business Logic

---

## Rules

- Never perform API requests.
- Never contain business logic.
- Keep validation close to the form.

---

## Output Expectations

The generated Form must

- be reusable
- separate validation from UI
- integrate with the presentation controller