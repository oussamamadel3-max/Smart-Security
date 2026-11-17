# State management stack

This framework uses **Riverpod only**.

- `state_management: riverpod` — locked. Do not generate BLoC/Cubit code.
- Session owner: `authSessionProvider` (`AuthSessionController`).
- Locale owner: `localeProvider`.
- Shared core lives in `packages/smart_security_app_core` — import via `package:smart_security_app_core/smart_security_app_core.dart`.

state_management: riverpod
