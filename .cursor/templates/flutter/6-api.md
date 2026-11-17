# API Template

## Purpose

Provide communication with remote services.

The API class is responsible only for HTTP requests.

---

## Responsibilities

- Send requests.
- Receive responses.
- Forward results to the Data Source.

---

## Structure

Generate:

- Feature API class
- Endpoint methods
- Request parameters
- Response handling

---

## Dependencies

Allowed

- ApiClient
- Dio

Forbidden

- UI
- Entity
- ViewModel

---

## Rules

- Never contain business logic.
- Never perform mapping.
- Never access Presentation.

---

## Output Expectations

The API implementation must

- remain focused on communication
- reuse shared networking infrastructure