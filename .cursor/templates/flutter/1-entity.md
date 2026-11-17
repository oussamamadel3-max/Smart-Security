# Entity Template

## Purpose

Represents the business object of the feature.

The Entity belongs to the Domain layer.

It must remain independent from external frameworks and infrastructure.

---

## Responsibilities

- Represent business data.
- Be immutable.
- Be reusable across the application.
- Be independent from Data and Presentation layers.

---

## Structure

Generate:

- Immutable class
- Value-based equality
- Required properties only
- Named constructors if necessary

Do not generate unnecessary members.

---

## Dependencies

Allowed

- freezed_annotation

Forbidden

- Flutter
- Dio
- JSON serialization
- Database libraries
- API models

---

## Rules

- Never expose DTOs.
- Never contain networking logic.
- Never contain persistence logic.
- Never depend on BuildContext.
- Never import Presentation or Data layers.

---

## Output Expectations

The generated Entity must

- represent business concepts only
- be framework independent
- be reusable
- compile without depending on Flutter