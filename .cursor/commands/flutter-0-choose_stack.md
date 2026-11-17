---
description: Lock the project to Riverpod or BLoC. Use before the first feature on a new app, or when the stack is still auto/undetected. Does not migrate an app that already uses one stack.
---

# Choose State Management Stack

Lock this project to **Riverpod** or **BLoC**.

## Instructions

1. Inspect `.cursor/docs/flutter/stack.md`, `pubspec.yaml`, and `lib/`.
2. If the app already uses one stack, keep it. Tell the user. Do not migrate unless they explicitly asked to migrate.
3. If unlocked (`auto` and undetectable), collect the user's choice: `riverpod` or `bloc`.
4. Write that value into `.cursor/docs/flutter/stack.md` (`state_management: riverpod` or `bloc`).
5. Do not generate feature code in this command.

## Expected Result

`.cursor/docs/flutter/stack.md` records exactly one stack. Later commands follow `.cursor/rules/flutter/state-management-riverpod.mdc` or `.cursor/rules/flutter/state-management-bloc.mdc`.
