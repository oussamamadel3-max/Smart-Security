# smart_security_app_core

حزمة Flutter مشتركة لتطبيقي `security_mobile` و`admin_web`.

## يشمل

- ثيم فاتح + Design Tokens من `design/index.html` (خط Cairo)
- أخطاء / Failure / Result / ErrorInterceptor
- Dio أساسي **بدون** AuthInterceptor
- ترجمة عامة (عربي افتراضي + إنجليزي)
- ويدجتات مشتركة (`MyDialog`, `MySnackbar`, …)
- عقود أدوار/صلاحيات مجردة

## لا يشمل

- Session / Secure Storage / Cookie / CSRF
- Router وGuards
- شاشات الأعمال وFCM وWi-Fi
- Endpoints أجهزة/أوامر (تُضاف بعد اعتماد العقود)

## الاستخدام

```yaml
dependencies:
  smart_security_app_core:
    path: ../packages/smart_security_app_core
```

```dart
ProviderScope(
  overrides: [
    appCoreConfigProvider.overrideWithValue(
      AppCoreConfig(baseUrl: 'https://api.example.com'),
    ),
    sharedPreferencesProvider.overrideWithValue(prefs),
  ],
  child: const MyApp(),
);
```

## قواعد الإضافة

| أضف إلى الحزمة | أبقِ في التطبيق |
|----------------|-----------------|
| توكنات ثيم، أخطاء عامة، ويدجتات عامة، مسارات Auth الافتراضية المؤكدة | Auth refresh، جلسة، توجيه، ميزات، FCM، CSRF |
