import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:security_mobile/features/devices/presentation/widgets/info_notice.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

void main() {
  testWidgets('InfoNotice renders warning and info tones', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: Column(
            children: [
              InfoNotice(text: 'info-msg'),
              InfoNotice(text: 'warn-msg', tone: InfoNoticeTone.warning),
            ],
          ),
        ),
        localizationsDelegates: [
          AppLocalizations.delegate,
        ],
      ),
    );

    expect(find.text('info-msg'), findsOneWidget);
    expect(find.text('warn-msg'), findsOneWidget);
  });
}
