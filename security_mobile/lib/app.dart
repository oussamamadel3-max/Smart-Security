import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import 'core/router/app_router.dart';
import 'features/notifications/presentation/providers/notifications_providers.dart';

class HisnMobileApp extends ConsumerWidget {
  const HisnMobileApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    // Keep push lifecycle + notification coordinators alive.
    ref.watch(pushTokenLifecycleProvider);
    ref.watch(notificationOpenCoordinatorProvider);
    ref.watch(foregroundAlertCoordinatorProvider);

    final locale = ref.watch(localeProvider);
    final router = ref.watch(appRouterProvider);

    return MaterialApp.router(
      title: 'حِصن',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.light,
      locale: locale,
      supportedLocales: kSupportedLocales,
      localizationsDelegates: const [
        AppLocalizations.delegate,
        GlobalMaterialLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
      ],
      routerConfig: router,
    );
  }
}
