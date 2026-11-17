# UI State Template

## Purpose

Define how the UI represents different application states.

The UI State controls rendering only.

It does not contain business logic or data manipulation.

---

## Responsibilities

Represent the current visual state of the screen.

Examples

- Initial
- Loading
- Content
- Empty
- Error
- Refreshing

Generate only the states required by the feature.

---

## Rendering Rules

The UI must always render exactly one primary state.

Avoid combining multiple independent loading or error indicators unless explicitly required.

State priority

Loading

↓

Error

↓

Empty

↓

Content

↓

Refreshing

---

## Integration

Integrate with

- Presentation controller
- State
- ErrorState
- Design System

Reuse existing UI components.

---

## Error Handling

Display errors using the project's Design System.

Select the appropriate presentation automatically.

Examples

- Inline Error
- Dialog
- Snackbar
- Full Screen Error

Use ErrorState to determine the presentation.

---

## Loading

Loading indicators should represent the current user action.

Examples

- Initial loading
- Refreshing
- Pagination loading
- Form submission

Avoid blocking the entire screen unless necessary.

---

## Empty State

Show an Empty State only when

- the request completed successfully
- no content exists

Do not display an Empty State during loading or while an error is active.

---

## Content State

Content is the default UI state.

Display

- data
- actions
- interactions

Do not mix business logic into the UI.

---

## Refreshing

Refreshing should preserve the existing content whenever possible.

Avoid replacing visible content with a loading screen.

---

## Rules

The UI should

- react only to State changes
- never perform business logic
- never call repositories
- never access DTOs
- never perform networking

Always keep rendering declarative.

---

## Output Expectations

The generated UI should

- remain predictable
- present only one primary visual state
- use the project's Design System
- integrate with ErrorState
- provide a consistent user experience