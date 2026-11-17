# Widget Template

## Purpose

Represent a reusable UI component.

Widgets should remain focused on presentation.

---

## Responsibilities

- Display UI.
- Receive input.
- Emit callbacks.
- Remain reusable.

---

## Structure

Generate

- StatelessWidget when possible.
- StatefulWidget only when required.
- Small focused widgets.

---

## Dependencies

Allowed

- Theme
- Localization
- Design System

Forbidden

- Repository
- API
- DTO
- Business Logic

---

## Rules

- Keep widgets small.
- Prefer composition over large widgets.
- Avoid duplicated UI.

---

## Output Expectations

The generated Widget must

- be reusable
- be testable
- remain presentation-only