import 'package:flutter_test/flutter_test.dart';
import 'package:security_mobile/core/router/app_routes.dart';
import 'package:security_mobile/features/shell/presentation/pages/owner_main_shell.dart';

void main() {
  test('shell tab locations stay under /app', () {
    expect(OwnerShellTabs.locationForIndex(0), AppRoutes.dashboard);
    expect(OwnerShellTabs.locationForIndex(1), AppRoutes.devices);
    expect(OwnerShellTabs.locationForIndex(2), AppRoutes.notifications);
    expect(OwnerShellTabs.locationForIndex(3), AppRoutes.profile);
  });
}
