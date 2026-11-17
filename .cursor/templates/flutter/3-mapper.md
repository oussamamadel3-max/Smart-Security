# Mapper Template

## Purpose

Convert data between DTOs and Entities.

The Mapper isolates external data models from business models.

---

## Responsibilities

- Convert DTO → Entity.
- Convert Entity → DTO when required.
- Normalize external data.

---

## Structure

Generate:

- One mapper per feature.
- Named conversion methods.
- Null-safe conversions when needed.

---

## Dependencies

Allowed

- DTO
- Entity

Forbidden

- Flutter
- Dio
- UI
- Repository

---

## Rules

- Never contain networking logic.
- Never perform API requests.
- Never modify business behavior.

---

## Output Expectations

The generated Mapper must

- be deterministic
- be reusable
- convert data without side effects