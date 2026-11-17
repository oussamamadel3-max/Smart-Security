# Presentation controller template

Coordinate the Presentation layer: receive user actions, run UseCases, expose immutable UI state.

Generate **one** controller per screen, using the stack resolved in `.cursor/rules/flutter/state-management.mdc`. Never generate both stacks. Never generate ViewModel + Notifier or ViewModel + Cubit.

---

## Responsibilities

- Manage screen state
- Execute UseCases
- Expose immutable state (`ErrorState`, Entity, loading)
- No Dio, API, DTO, Widgets, or BuildContext

---

## Riverpod

Generate `LoginViewModel extends AsyncNotifier<LoginState>` (or `Notifier` when not async). The ViewModel **is** the Notifier.

```dart
class LoginViewModel extends AsyncNotifier<LoginState> {
  @override
  LoginState build() => const LoginState();

  Future<void> submit({...}) async { /* UseCase → Result → state */ }
}

final loginViewModelProvider =
    AsyncNotifierProvider<LoginViewModel, LoginState>(LoginViewModel.new);
```

Dispose CancelTokens in `ref.onDispose()`.

Folder: `presentation/viewmodels/`.

---

## BLoC

Default: **Cubit**. Use `Bloc` only when events need names or transformers (see `.cursor/rules/flutter/state-management-bloc.mdc`).

```dart
class LoginCubit extends Cubit<LoginState> {
  LoginCubit(this._loginUseCase) : super(const LoginState());
  final LoginUseCase _loginUseCase;
  CancelToken? _cancelToken;

  Future<void> submit({...}) async {
    emit(state.copyWith(isSubmitting: true, clearError: true));
    final result = await _loginUseCase(...);
    if (isClosed) return;
    result.fold(
      onFailure: (f) => emit(state.copyWith(
        isSubmitting: false,
        error: FailureHandler.handle(f, onRetry: () => submit(...)),
      )),
      onSuccess: (_) => emit(state.copyWith(isSubmitting: false)),
    );
  }

  @override
  Future<void> close() {
    _cancelToken?.cancel();
    return super.close();
  }
}
```

Page: `BlocProvider(create: ...)` on the route. Build with `BlocSelector`. Snackbar/navigation with `BlocListener`. `onPressed` uses `context.read<LoginCubit>()`.

Folder: `presentation/cubits/` (or `presentation/blocs/` when a Bloc is justified).

When a **Bloc** is required (search debounce, droppable submit), events are sealed/freezed, have no `BuildContext`, and use `bloc_concurrency` transformers.

---

## Rules

- Never call APIs or DataSources
- Never access Repositories directly if UseCases exist
- Never manipulate Widgets
- One feature, one controller class
