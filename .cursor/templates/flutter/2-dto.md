# DTO Template

## Purpose

Represents data exchanged with external sources.

DTOs belong exclusively to the Data layer.

---

## Responsibilities

- Represent request and response models.
- Serialize and deserialize data.
- Transfer data between the API and the application.

---

## Structure

Generate:

- Data model
- Serialization support
- Required properties only
- Optional fields when supported by the API

---

## Dependencies

Allowed

- Serialization libraries
- Data layer utilities

Forbidden

- Flutter
- BuildContext
- Business logic
- Presentation layer

---

## Rules

- Never expose DTOs outside the Data layer.
- Never contain business logic.
- Never access APIs directly.
- Never replace Entities.

---

## Output Expectations

The generated DTO must

- accurately represent the API contract
- be serializable
- remain isolated inside the Data layer