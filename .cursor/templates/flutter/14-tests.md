# Tests Template

## Purpose

Prove Domain and Data behavior. A feature that only compiles is not done.

---

## Order

Write tests in this order. Stop at the last layer that changed.

1. UseCase
2. Repository
3. Mapper
4. Presentation controller (ViewModel or Cubit)
5. Widget — only if the widget has logic beyond rendering

---

## Rules

- Mock external dependencies (API, Isar, secure storage). Never mock the business rule under test.
- Cover success and the error-mapping path (`Failure` subtype, not a generic catch). Cubit/Bloc: `bloc_test`.
- Optimistic write: force remote failure and assert rollback to the exact prior state.
- Pagination: filter-change while a page is loading cancels the stale request.
- Auth: refresh-fails transitions the session owner to `sessionExpired`.

Place tests next to the Flutter app's existing test layout. If none exists yet, use:

```text
test/features/<feature>/
  domain/<name>_usecase_test.dart
  data/<name>_repository_test.dart
  data/<name>_mapper_test.dart
  presentation/<name>_viewmodel_test.dart  # Riverpod
  presentation/<name>_cubit_test.dart      # BLoC
```

---

## Example (UseCase)

```dart
test('returns entity on success', () async {
  when(() => repository.getById('1'))
      .thenAnswer((_) async => Result.success(course));
  final result = await useCase('1');
  expect(result, isA<Success<CourseEntity>>());
});

test('propagates failure', () async {
  when(() => repository.getById('1'))
      .thenAnswer((_) async => const Result.failure(Failure.network()));
  final result = await useCase('1');
  expect(result.failureOrNull, isA<NetworkFailure>());
});
```

## Example (Cubit)

```dart
blocTest<LoginCubit, LoginState>(
  'emits submitting then error on failure',
  build: () {
    when(() => loginUseCase(...)).thenAnswer(
      (_) async => const Result.failure(Failure.network()),
    );
    return LoginCubit(loginUseCase);
  },
  act: (cubit) => cubit.submit(username: 'a', password: 'b', providerId: '1'),
  expect: () => [
    isA<LoginState>().having((s) => s.isSubmitting, 'submitting', true),
    isA<LoginState>().having((s) => s.error, 'error', isNotNull),
  ],
);
```

---

## Output Expectations

New or changed operations have passing UseCase and Repository tests before the task is reported done.
