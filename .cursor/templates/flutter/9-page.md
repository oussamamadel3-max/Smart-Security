# Page Template

## Purpose

Represent a single application screen.

The Page is responsible only for composing the UI.

---

## Responsibilities

- Build the layout.
- Connect the presentation controller (ViewModel **or** Cubit/Bloc).
- Display UI state.
- Trigger user actions.

---

## Structure

Generate

- One page
- Responsive layout
- Minimal build method

Extract reusable UI into Widgets.

---

## Dependencies

Allowed

- Presentation controller (ViewModel or Cubit/Bloc)
- Widgets
- Theme
- Localization

Forbidden

- Repository
- API
- DTO
- Business Logic

---

## Rules

- Never contain business logic.
- Never call APIs.
- Never parse data.
- Never manipulate DTOs.

---

## Output Expectations

The generated Page must

- remain lightweight
- compose reusable widgets
- react to controller state (Riverpod: `ref.watch` / `select`; BLoC: `BlocSelector`/`BlocBuilder` to build, `BlocListener` for snackbar/navigation, `context.read` in callbacks)