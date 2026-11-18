# نظام الأمان الذكي — Agent Operating Instructions

> Scope: development, bug fixes, reviews, tests, and SRS-aligned work across this monorepo. The user’s task specifies **what** to do; these instructions and the project’s planning files specify **how**. These instructions do not authorize unrelated changes.

## Language and reporting

- Keep agent-facing technical rules, source-code symbols, paths, API contracts, MQTT topics/payload fields, permission keys, and framework terminology in **English**.
- Respond to the developer in **Arabic** by default. Explain technical concepts in Arabic while retaining precise English terms. Keep end-user API / UI messages in Arabic under the existing contract.
- Never invent code locations, tests performed, requirements, decisions, or results. Clearly distinguish verified repository facts from proposals.
- Preserve existing comments and names, including historical typos; do not reformat or rename unrelated code.

## System map (projects)

| Folder | Role |
|--------|------|
| `security_backEnd` | ASP.NET API, Identity, MQTT workers, Outbox, FCM server, PostgreSQL |
| `security_esp` | ESP32 hardware + firmware, local alarm path |
| `security_mobile` | Owner Flutter app, FCM client, local Wi-Fi provisioning |
| `admin_web` | Flutter Web (owner + admin) |
| `integration` | Shared MQTT/HTTP contracts, G1/G2 gates, acceptance matrix, DoD |
| `ops` | Compose, environments, deploy, backup, field ops |

Root orientation: `system-context.md` (identity / blockers / dependencies) and `system-progress.md` (counts / timeline / naming legend). Per-project files use prefixes `backend-`, `esp-`, `mobile-`, `web-`, `integration-`, `ops-`.

## Authoritative sources and precedence

1. Follow the user’s **explicit task and approved scope**. Do not silently expand an FR into other requirements or other projects.
2. For a requirements task, read the FR/BR/NFR and linked rules in `docs/SRS_نظام_الأمان_الذكي_v1.0.md`, plus the task card in the project’s `*-development-tasks.md`. If the Markdown SRS conflicts with `SRS_نظام_الأمان_الذكي_v1.0.docx` (repo root), **the Word document is authoritative**. Read the relevant passage before deciding. Do not guess from an inaccessible document. Before applying an approved requirements change, verify which source is authoritative and synchronize the corresponding Word + Markdown copies; if the authoritative Word file is unavailable, explicitly label the Markdown edit as **pending Word synchronization** and do not claim the authoritative document was updated or the discrepancy resolved.
3. Supporting plans: `docs/خطة_تطوير_نظام_الأمان_الذكي_v1.0.md`, `docs/تصميم_قاعدة_بيانات_نظام_الأمان_الذكي_v1.0.md`, `docs/مواصفات_نظام_الأمان_الذكي.md`, `docs/EXECUTION_SEQUENCE.md`. Closing any task: `integration/integration-definition-of-done.md` and, for shared acceptance, `integration/integration-acceptance-matrix.md`.
4. `*-current.md` tracks the active task sheet only; it is not a substitute for the SRS. The repository code is evidence of implemented behavior; it does not supersede approved requirements.
   - **Document synchronization workflow:** identify the approved source and affected mirrored `.md`/`.docx` files; make equivalent changes in each; verify the same rule, examples, and revision note; record any unavailable source as pending; do not mark the documentation update complete until the authoritative original and its mirrors agree. Cross-project MQTT schemas also require `integration` review and G1 acceptance before production use.
5. On an unresolved conflict affecting ownership isolation, session invalidation, data retention, MQTT/HTTP contracts, or gate policy (`G0`/`G1`/`G2`): state the exact conflict and block the unsafe decision. Do not invent production defaults for unresolved portions of `D-01`…`D-07` or pending G1 contract details. `D-09` already selects a full actual-state `COMMAND_ACK` with `resultStateSequence`; its exact payload/schema and ACK/STATE ordering tests still require G1 adoption. `D-03` / `NFR-008` still require measured event capacity and positive command TTL.

## Planning file roles (do not conflate)

| Pattern | Meaning |
|---------|---------|
| `*-context.md` | Where we are and why (identity, principles, blockers) |
| `*-progress.md` | How much / when (counts, timeline, checkpoints) |
| `*-current.md` | What we are doing now (required / acceptance / done / verify) |
| `*-development-tasks.md` | Official T-xx backlog and `[ ]` / `[x]` closure |
| `*-development-plan.md` | Phase scope; what exists vs what is built |

Mark `[x]` in `*-development-tasks.md` only after acceptance and DoD. Update progress/context after concrete milestones; never put secrets in planning files.

### Temporary exception — Autonomous Sequential Mobile Execution (owner-authorized, 2026-11-17)

When the owner issues an **Autonomous Sequential Mobile Development** prompt covering `MOB-T-02`…`MOB-T-08` (or explicitly re-authorizes / restores that mode — including “cancel the APPROVE TASK CLOSURE gate so the prompt continues”):

1. **`APPROVE TASK CLOSURE` is suspended for `MOB-T-*` in this sequence.** After evidenced client-scope acceptance, the agent **must auto-close** (mark proved `[x]`, update `mobile-*` / `system-*`) and **must continue immediately** to the next eligible `MOB-T-*` without waiting for any closure message.
2. Save the task report under `docs/reports/mobile/` noting this exception.
3. **G0/G1, Backend/ESP/shared-contract adoption, device/emulator/iOS runs** are **out of scope** for this sequence when the owner prompt already forbade them. Leave those cards unchecked or mark `BLOCKED` / `NOT VERIFIED` in the report; **do not stop the chain** and **do not** invent G0/G1 adoption or cross-project changes.
4. Stop the chain only on: requirements conflict inside mobile scope, missing secret the owner did not defer, or an **unfixable in-scope mobile failure** after a fix attempt.
5. This exception is **mobile-only** for that sequence. Default Section 6 (`APPROVE TASK CLOSURE`) remains for Backend, ESP, Integration, Ops — and for Web **unless** the Web exception below is active.
6. The owner may revoke this exception at any time; thereafter default closure gates resume.

The detailed procedure lives in `.cursor/rules/common/task-execution.mdc` §6A.

### Temporary exception — Autonomous Sequential Web Execution (owner-authorized, 2026-11-17)

When the owner issues an **Autonomous Sequential Web Development** prompt covering `WEB-T-02`…`WEB-T-08` (or explicitly re-authorizes / restores that mode):

1. **`APPROVE TASK CLOSURE` is suspended for `WEB-T-*` in this sequence.** After evidenced client-scope acceptance, the agent **must auto-close** (mark proved `[x]`, update `web-*` / `system-*`) and **must continue immediately** to the next eligible `WEB-T-*` without waiting for any closure message.
2. Save the task report under `docs/reports/web/` noting this exception.
3. **G0/G1 adoption, Backend/ESP/shared-contract edits, live browser CSRF/CORS against production, and deploy** are **out of scope** when the owner prompt forbids them. Leave those cards unchecked or mark `BLOCKED` / `NOT VERIFIED`; **do not stop the chain** and **do not** invent gate closures or cross-project changes.
4. Stop the chain only on: requirements conflict inside web scope, missing secret the owner did not defer, absent mandatory API for acceptance that cannot be software-completed, or an **unfixable in-scope web failure** after a fix attempt.
5. This exception is **web-only**. It does not authorize Backend, ESP, Mobile, Integration, or Ops auto-close.
6. The owner may revoke this exception at any time; thereafter default closure gates resume.

## Work discipline

- First inspect the affected project’s `*-context.md`, `*-current.md`, task card, and the real code/contracts involved. Find a comparable implementation when useful.
- Define acceptance criteria from the request and relevant SRS section. For nontrivial work, identify impacted layers/projects, risks, gate dependencies, and verification before editing.
- Implement the **smallest complete and correct change**. Preserve established patterns when appropriate, but never reproduce a demonstrated bug or security flaw just for consistency.
- Do not redesign libraries, rename files, replace storage engines, broaden public contracts, change historical migrations, or introduce architectural refactors incidentally.
- Stay inside the assigned project unless the user explicitly expands scope. Cross-project contract changes belong in `integration` first.
- Do not confuse build success with behavioral correctness. Add or update relevant tests, review the diff, and report actual verification.
- Never run production database migrations or modify shared/production data without explicit authorization. Never commit or stage files unless the user requests a Git action; also respect the staging policy below.

## Non-negotiable product and security boundaries

- **Local safety path is mandatory:** sensor → ESP32 → siren must work without internet/server when ARMED. The server must not replace local alarm behavior.
- **Source of truth:** last device-confirmed state (`ACK` / `STATE`). HTTP success or broker delivery alone is not confirmation.
- Flutter apps **must not** speak MQTT directly; they use HTTPS to the backend.
- Enforce **authentication + ownership (or admin policy) + operation-specific business rules** on protected operations. Role names alone are not authorization checks.
- Prevent cross-owner reads/writes by filtering in queries and validating on the server. Admin paths follow their own defined policy — do not invent unrestricted access.
- On owner disable/archive: invalidate revocable sessions and re-check `is_active` (or equivalent) on protected operations.
- Soft-delete / archive policies: do not physically destroy retained operational history contrary to domain rules; distinguish cleanup of **uncommitted** orphaned artifacts after failure.
- **No production command/event paths** closed before `G1` message-contract adoption in `integration`.
- **No final production acceptance** before `G2` and the shared acceptance matrix.
- Do not ship production firmware before `G0` closure on `D-01`…`D-07`.
- Do not hard-code guessed production timeouts/TTL/capacities while `D-03` / `NFR-008` remain open.
- No secrets in Git, planning markdown, or committed configs.

## Backend (`security_backEnd`) — fixed stack and architecture

- Solution: `security_backEnd/Backend.sln` — `.NET 10`, ASP.NET Core, EF Core, PostgreSQL/Npgsql, ASP.NET Core Identity (`uuid` / Guid keys), Mapster. Verify against the current branch before assuming a component exists.
- Dependency direction: `API → Application → Domain` and `Infrastructure → Application / Domain`; `Domain` must not depend on outer layers. Controllers call Application services; Application uses repository/unit-of-work **interfaces**, not `DbContext` directly. See `security_backEnd/docs/ARCHITECTURE.md`.
- Preserve unified `ApiResponse` success/error JSON; file download/export endpoints may return `File(...)` when that pattern exists.
- Prefer exception-driven errors + central middleware; register new services/repos as `Scoped` in the existing DI registration.
- Command closure requires device ACK or an explicit timeout/reconciliation outcome — not HTTP alone.
- Persist notification request in the **same transaction** as event save where the SRS requires it; delivery workers run after commit.

## ESP / mobile / web / ops notes

- `security_esp`: treat SW-420 as vibration unless a suitable door sensor is adopted; local alarm acceptance needs real hardware evidence, not API green builds alone.
- `security_mobile` / `admin_web`: follow project `*-development-plan.md`; use the implemented Backend T-02 auth endpoints as the initial source for client contracts (JWT Access, Web Cookie/CSRF, Mobile Body Refresh); verify the actual OpenAPI and have `integration` adopt the shared HTTP contract. Do not wait for MQTT G1 to implement authentication clients.
- `ops`: local Compose/Mosquitto and environments support development; field/VPS work stays in ops tasks — do not improvise production deploy during feature tasks.
- `integration`: owns shared MQTT/HTTP drafts, G1 finalization of the already selected `D-09` ACK semantics, and gates `G1`/`G2`; other projects consume adopted contracts rather than forking them silently.

## Git and task artifacts

- Do not create a commit unless explicitly requested.
- Do not stage these without explicit project-owner authorization changing this policy: `AGENTS.md`, `system-context.md`, `system-progress.md`, all `*-context.md` / `*-progress.md` / `*-current.md` / `*-development-plan.md` / `*-development-tasks.md`, `docs/` planning extracts, `integration/integration-*.md`, and any future `.cursor/` rules or skills.
- Updating progress files on disk is different from staging/committing them. Never silently stage excluded guidance or task metadata.

## Completion requirements

1. Explain in Arabic what changed and why, listing actual changed/new files and which project folder they belong to.
2. Report impact on auth/session invalidation, ownership isolation, schema/migrations, retention, MQTT/HTTP contracts, and gates (`G0`/`G1`/`G2`) when relevant; explicitly say when there was no such impact.
3. Show build, tests, and diff checks **actually run**, with counts/results and any blocked or unrun checks. Give reproducible verification steps (Swagger/OpenAPI, authenticated session + CSRF where used, MQTT ACK path where relevant).
4. End development, bug-fixing, testing, and code-review responses with **🧠 مقتطف هندسي من المهمة**: one fact-based technical lesson, why/when it applies, a verified class/file reference, and one practical check. Aim for 60–120 Arabic words; do not invent a lesson if no code was inspected.
)
## Cross-project consistency checkpoint (2026-11-04)

- Auth Backend T-02 is documented as completed **according to Cursor test evidence supplied by the user** (59 integration + 1 unit passed); cross-project static documentation review did not independently inspect the live code repository. Backend T-03 remains not started.
- Initial device linking: PENDING until acknowledged. Ownership transfer: new owner becomes ACTIVE on the server; if the device is connected, the new owner may submit a command request subject to server-side checks, but **no publish/execution before ownership-version acknowledgment**.
- Release gate G2 requires success of all 25 acceptance scenarios under development plan §12, not merely the four critical scenarios. Exception policy changes require explicit amendment of the authoritative Word and Markdown documents.
- Word originals must be checked/synchronized; changes to Markdown in this cross-project review are **pending authoritative Word verification** where mirrored requirements are involved.
